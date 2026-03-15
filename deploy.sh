#!/bin/bash
# Music Grabber Production Deployment Script
# This script helps deploy the application with Traefik and SSL

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Music Grabber Deployment Script${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed${NC}"
    exit 1
fi

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Error: Docker Compose is not installed${NC}"
    exit 1
fi

# Check Traefik network
echo -e "${YELLOW}Checking Traefik network...${NC}"
if ! docker network ls | grep -q "traefik"; then
    echo -e "${YELLOW}Traefik network not found. Creating...${NC}"
    docker network create traefik
    echo -e "${GREEN}✓ Traefik network created${NC}"
else
    echo -e "${GREEN}✓ Traefik network exists${NC}"
fi

echo ""
echo -e "${YELLOW}Step 1: Environment Configuration${NC}"
echo "========================================"

# Check if .env exists
if [ ! -f .env ]; then
    echo -e "${YELLOW}Creating .env file from template...${NC}"
    cp .env.example .env
    echo -e "${RED}⚠️  Please edit .env file with your credentials!${NC}"
    echo "Required:"
    echo "  - YOUTUBE_API_KEY"
    echo "  - GOOGLE_CLIENT_ID"
    echo "  - GOOGLE_CLIENT_SECRET"
    echo "  - SMTP_PASSWORD (optional)"
    echo ""
    echo "Edit the file now? (y/n)"
    read -r response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        nano .env
    else
        echo -e "${RED}Please edit .env manually before continuing${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ .env file exists${NC}"
fi

echo ""
echo -e "${YELLOW}Step 2: DNS Configuration Check${NC}"
echo "========================================"
echo "Domain: mgrabber.home.freaxnx01.ch"
echo ""
echo "Make sure you have:"
echo "1. Created A record: mgrabber.home.freaxnx01.ch → YOUR_EXTERNAL_IP"
echo "2. Port forwarded 443 → 192.168.1.124:443 (Traefik)"
echo "3. Port forwarded 80 → 192.168.1.124:80 (Traefik redirect)"
echo ""

# Get external IP
EXTERNAL_IP=$(curl -s ifconfig.me)
echo "Your external IP appears to be: $EXTERNAL_IP"
echo ""

echo "Ready to deploy? (y/n)"
read -r response
if [[ ! "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    echo -e "${RED}Deployment cancelled${NC}"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 3: Building and Deploying${NC}"
echo "========================================"

# Pull latest changes
echo "Pulling latest changes..."
git pull origin main

# Stop existing containers
echo "Stopping existing containers..."
docker-compose down

# Build and start with production config
echo "Building and starting services..."
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Wait for services to start
echo "Waiting for services to start..."
sleep 10

echo ""
echo -e "${YELLOW}Step 4: Health Check${NC}"
echo "========================================"

# Check API health
API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8085/api/health || echo "000")
if [ "$API_HEALTH" = "200" ]; then
    echo -e "${GREEN}✓ API is healthy${NC}"
else
    echo -e "${RED}✗ API health check failed (HTTP $API_HEALTH)${NC}"
    echo "Check logs: docker-compose logs download-api"
fi

# Check if Traefik picked up the route
echo ""
echo -e "${YELLOW}Traefik route check:${NC}"
echo "Checking if Traefik has registered the route..."
docker logs traefik 2>&1 | grep -i "mgrabber" | tail -5 || echo "Route may still be propagating"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Access your application:"
echo "  • Frontend: https://mgrabber.home.freaxnx01.ch"
echo "  • API Health: http://192.168.1.124:8085/api/health"
echo ""
echo "Useful commands:"
echo "  • View logs: docker-compose logs -f"
echo "  • Restart: docker-compose restart"
echo "  • Stop: docker-compose down"
echo ""
echo -e "${YELLOW}Note: SSL certificate may take a few minutes to provision${NC}"
echo ""

# Update GitHub issue if token available
if [ -n "$GITHUB_TOKEN" ]; then
    echo "Would you like to update Issue #6 as completed? (y/n)"
    read -r update_issue
    if [[ "$update_issue" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        curl -s -X PATCH \
            -H "Authorization: token $GITHUB_TOKEN" \
            -H "Accept: application/vnd.github.v3+json" \
            https://api.github.com/repos/freaxnx01/mgrabber-nextgen/issues/6 \
            -d '{"state":"closed"}' > /dev/null
        echo -e "${GREEN}✓ Issue #6 marked as closed${NC}"
    fi
fi
