.PHONY: build run watch stop test test-unit test-integration coverage \
       docker-build docker-run up down logs rebuild deploy-test push-image \
       migrate migration-add lint outdated vuln check-yt-key clean help

IMAGE_NAME := musicgrabber
IMAGE_TAG := local
GHCR_IMAGE := ghcr.io/freaxnx01/mgrabber-nextgen:main

COMPOSE := docker compose -f docker-compose.yml -f docker-compose.override.yml
HOST_PROJECT := src/Host/Host.csproj
STARTUP_PROJECT := src/Host

# ── Development ──────────────────────────────────────────────────────────────

build: ## Build solution in Release mode
	$(call tee_log,dotnet build -c Release)

stop: ## Stop docker-compose and free port 8086
	@$(COMPOSE) down 2>/dev/null || true
	@PID=$$(lsof -ti :8086 2>/dev/null); \
	if [ -n "$$PID" ]; then \
		echo "── Killing process on port 8086 (PID $$PID)"; \
		kill $$PID 2>/dev/null || true; \
	fi

LOG_DIR  := logs
LOG_FILE := $(LOG_DIR)/dev.log

# Usage: $(call tee_log,command)
tee_log = mkdir -p $(LOG_DIR) && $(1) 2>&1 | tee -a $(LOG_FILE)

run: stop .env ## Run Host locally (no Docker)
	@echo "── SSH tunnel (run in WSL2 on Win11): ssh -N -L 8086:localhost:8086 freax@192.168.1.108"
	@echo "── Logs: $(LOG_FILE)"
	$(call tee_log,set -a && . ./.env && set +a && ASPNETCORE_ENVIRONMENT=Development dotnet run --project $(HOST_PROJECT) --urls http://localhost:8086)

watch: stop .env ## Run Host with hot reload
	@echo "── SSH tunnel (run in WSL2 on Win11): ssh -N -L 8086:localhost:8086 freax@192.168.1.108"
	@echo "── Logs: $(LOG_FILE)"
	$(call tee_log,set -a && . ./.env && set +a && ASPNETCORE_ENVIRONMENT=Development dotnet watch --non-interactive --project $(HOST_PROJECT) --urls http://localhost:8086)

# ── Testing ──────────────────────────────────────────────────────────────────

test: ## Run all tests
	$(call tee_log,dotnet test)

test-unit: ## Run unit tests only
	$(call tee_log,dotnet test tests/Download.UnitTests && dotnet test tests/Discovery.UnitTests && dotnet test tests/Radio.UnitTests && dotnet test tests/Quota.UnitTests && dotnet test tests/Identity.UnitTests && dotnet test tests/Shared.UnitTests)

test-integration: ## Run integration tests only
	$(call tee_log,dotnet test tests/Download.IntegrationTests)

coverage: ## Run tests with code coverage
	$(call tee_log,dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage)
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

push-image: ## Build and push Docker image to GHCR (skips CI)
	docker build -t $(GHCR_IMAGE) .
	docker push $(GHCR_IMAGE)

docker-build: ## Build Docker image
	docker build -t $(IMAGE_NAME):$(IMAGE_TAG) .

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
	$(call tee_log,cd $(STARTUP_PROJECT) && dotnet ef database update --project ../Modules/Download/Infrastructure --context DownloadDbContext && dotnet ef database update --project ../Modules/Identity/Infrastructure --context IdentityDbContext && dotnet ef database update --project ../Modules/Quota/Infrastructure --context QuotaDbContext)

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
	$(call tee_log,dotnet format --verify-no-changes)

outdated: ## Check for outdated packages
	$(call tee_log,dotnet list package --outdated)

vuln: ## Check for vulnerable packages
	$(call tee_log,dotnet list package --vulnerable --include-transitive)

# ── Verification ────────────────────────────────────────────────────────

check-yt-key: .env ## Verify YouTube Data API v3 key is valid
	@YOUTUBE_API_KEY=$$(grep -E '^YOUTUBE_API_KEY=' .env | cut -d= -f2-); \
	if [ -z "$$YOUTUBE_API_KEY" ]; then \
		echo "ERROR: YOUTUBE_API_KEY is empty in .env"; \
		exit 1; \
	fi; \
	echo "── Testing YouTube Data API v3 key..."; \
	BODY=$$(curl -s -w '\n%{http_code}' \
		"https://www.googleapis.com/youtube/v3/videos?part=id&id=dQw4w9WgXcQ&key=$$YOUTUBE_API_KEY"); \
	RESPONSE=$$(echo "$$BODY" | tail -1); \
	BODY=$$(echo "$$BODY" | sed '$$d'); \
	if [ "$$RESPONSE" = "200" ]; then \
		echo "── PASS: API key is valid (HTTP 200)"; \
	elif [ "$$RESPONSE" = "403" ]; then \
		REASON=$$(echo "$$BODY" | grep -o '"reason":"[^"]*"' | head -1 | cut -d'"' -f4); \
		if [ "$$REASON" = "API_KEY_HTTP_REFERRER_BLOCKED" ]; then \
			echo "── FAIL: API key has HTTP referrer restrictions in Google Cloud Console."; \
			echo "         Server-side keys should use IP restrictions (or none), not referrer restrictions."; \
			echo "         Fix at: https://console.cloud.google.com/apis/credentials"; \
		else \
			MSG=$$(echo "$$BODY" | grep -o '"message":"[^"]*"' | head -1 | cut -d'"' -f4); \
			echo "── FAIL: HTTP 403 — $$MSG"; \
		fi; \
		exit 1; \
	elif [ "$$RESPONSE" = "400" ]; then \
		echo "── FAIL: Bad request — key may be malformed (HTTP 400)"; \
		exit 1; \
	else \
		echo "── FAIL: Unexpected response (HTTP $$RESPONSE)"; \
		exit 1; \
	fi

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
