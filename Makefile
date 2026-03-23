.PHONY: build run watch test test-unit test-integration coverage \
       docker-run up down logs rebuild deploy-test \
       migrate migration-add lint outdated vuln clean help

IMAGE_NAME := musicgrabber
IMAGE_TAG := local

COMPOSE := docker compose -f docker-compose.yml -f docker-compose.override.yml
HOST_PROJECT := src/Host/Host.csproj
STARTUP_PROJECT := src/Host

# ── Development ──────────────────────────────────────────────────────────────

build: ## Build solution in Release mode
	dotnet build -c Release

run: ## Run Host locally (no Docker)
	dotnet run --project $(HOST_PROJECT)

watch: ## Run Host with hot reload
	dotnet watch --project $(HOST_PROJECT)

# ── Testing ──────────────────────────────────────────────────────────────────

test: ## Run all tests
	dotnet test

test-unit: ## Run unit tests only
	dotnet test tests/Download.UnitTests \
	&& dotnet test tests/Discovery.UnitTests \
	&& dotnet test tests/Radio.UnitTests \
	&& dotnet test tests/Quota.UnitTests \
	&& dotnet test tests/Identity.UnitTests \
	&& dotnet test tests/Shared.UnitTests

test-integration: ## Run integration tests only
	dotnet test tests/Download.IntegrationTests

coverage: ## Run tests with code coverage
	dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
	@echo "Coverage reports in ./coverage/"

# ── Docker ───────────────────────────────────────────────────────────────────

docker-run: .env ## Run with docker-compose in foreground (Ctrl+C to stop)
	$(COMPOSE) up --build

up: .env ## Start in background with docker-compose
	$(COMPOSE) up -d --build

down: ## Stop docker-compose
	$(COMPOSE) down

logs: ## Follow container logs
	$(COMPOSE) logs -f

rebuild: down up ## Rebuild and restart

deploy-test: ## Build image and verify it starts + responds to health check
	@echo "── Building Docker image..."
	docker build -t $(IMAGE_NAME):$(IMAGE_TAG) .
	@echo "── Starting container..."
	docker rm -f mgrabber-deploy-test 2>/dev/null || true
	docker run -d --name mgrabber-deploy-test \
		-p 18080:8080 \
		-e ConnectionStrings__Default="Data Source=/data/musicgrabber.db" \
		-e GOOGLE_CLIENT_ID=test \
		-e GOOGLE_CLIENT_SECRET=test \
		-v /tmp/mgrabber-test-data:/data \
		-v /tmp/mgrabber-test-storage:/storage \
		$(IMAGE_NAME):$(IMAGE_TAG)
	@echo "── Waiting for startup..."
	@for i in 1 2 3 4 5 6 7 8 9 10; do \
		sleep 2; \
		STATUS=$$(curl -sk -o /dev/null -w '%{http_code}' http://localhost:18080/health/live 2>/dev/null); \
		if [ "$$STATUS" = "200" ] || [ "$$STATUS" = "500" ]; then \
			echo "── Container responding (HTTP $$STATUS)"; \
			break; \
		fi; \
		echo "── Waiting... (attempt $$i)"; \
	done
	@STATUS=$$(curl -sk -o /dev/null -w '%{http_code}' http://localhost:18080/health/live 2>/dev/null); \
	docker logs mgrabber-deploy-test 2>&1 | tail -5; \
	docker rm -f mgrabber-deploy-test > /dev/null 2>&1; \
	rm -rf /tmp/mgrabber-test-data /tmp/mgrabber-test-storage; \
	if [ "$$STATUS" = "200" ]; then \
		echo "── PASS: Health check returned 200"; \
	else \
		echo "── FAIL: Health check returned $$STATUS"; \
		exit 1; \
	fi

# ── Database ─────────────────────────────────────────────────────────────────

migrate: ## Run all EF Core migrations
	dotnet ef database update --project src/Modules/Download/Infrastructure --startup-project $(STARTUP_PROJECT)
	dotnet ef database update --project src/Modules/Identity/Infrastructure --startup-project $(STARTUP_PROJECT)
	dotnet ef database update --project src/Modules/Quota/Infrastructure --startup-project $(STARTUP_PROJECT)

migration-add: ## Add migration (NAME=xxx MODULE=Download|Identity|Quota)
ifndef NAME
	$(error NAME is required. Usage: make migration-add NAME=AddSomething MODULE=Download)
endif
ifndef MODULE
	$(error MODULE is required. Usage: make migration-add NAME=AddSomething MODULE=Download)
endif
	dotnet ef migrations add $(NAME) \
		--project src/Modules/$(MODULE)/Infrastructure \
		--startup-project $(STARTUP_PROJECT)

# ── Quality ──────────────────────────────────────────────────────────────────

lint: ## Check code formatting
	dotnet format --verify-no-changes

outdated: ## Check for outdated packages
	dotnet list package --outdated

vuln: ## Check for vulnerable packages
	dotnet list package --vulnerable --include-transitive

# ── Cleanup ──────────────────────────────────────────────────────────────────

clean: ## Remove build artifacts
	find . -type d \( -name bin -o -name obj -o -name publish \) -exec rm -rf {} + 2>/dev/null || true
	rm -rf coverage/

# ── Helpers ──────────────────────────────────────────────────────────────────

.env:
	@echo "ERROR: .env file not found. Create one with your secrets:"
	@echo ""
	@echo "  GOOGLE_CLIENT_ID=..."
	@echo "  GOOGLE_CLIENT_SECRET=..."
	@echo "  YOUTUBE_API_KEY=..."
	@echo "  SMTP_HOST=..."
	@echo "  SMTP_PORT=..."
	@echo "  SMTP_PASSWORD=..."
	@echo "  SMTP_FROM=..."
	@echo ""
	@echo "See docker-compose.yml for all variables."
	@exit 1

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

.DEFAULT_GOAL := help
