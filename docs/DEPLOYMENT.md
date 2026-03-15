# Deployment Guide - Music Grabber

## Quick Start

```bash
# 1. Clone and navigate
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen

# 2. Create environment file
cp .env.example .env

# 3. Edit .env with your credentials (see below)
nano .env

# 4. Deploy
docker-compose down
docker-compose up -d --build

# 5. Verify
# Frontend: https://mgrabber.home.freaxnx01.ch
# API Health: http://192.168.1.124:8085/api/health
```

---

## Prerequisites

### 1. Google Cloud Setup (Required)

**YouTube Data API v3:**
- Project: `music-grabber-prod`
- API Key: [From Google Cloud Console]
- Quota: 10,000 units/day (free)

**Google OAuth 2.0:**
- Client ID: [From Google Cloud Console]
- Client Secret: [From Google Cloud Console]
- Authorized redirect URI: `https://mgrabber.home.freaxnx01.ch/signin-google`

### 2. DNS Configuration

Create A record:
```
Type: A
Name: mgrabber.home
Value: YOUR_EXTERNAL_IP
TTL: 300
```

Get your external IP:
```bash
curl ifconfig.me
```

### 3. Traefik Configuration

Add labels to docker-compose.yml for frontend service:

```yaml
services:
  frontend:
    # ... existing config ...
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.mgrabber.rule=Host(`mgrabber.home.freaxnx01.ch`)"
      - "traefik.http.routers.mgrabber.entrypoints=websecure"
      - "traefik.http.routers.mgrabber.tls=true"
      - "traefik.http.routers.mgrabber.tls.certresolver=letsencrypt"
      - "traefik.http.services.mgrabber.loadbalancer.server.port=8080"
      - "traefik.docker.network=traefik"
    networks:
      - internal
      - traefik  # Add your Traefik network
```

Add network to docker-compose.yml:
```yaml
networks:
  internal:
  traefik:
    external: true  # Your existing Traefik network
```

### 4. Router Port Forwarding

Forward external port 443 to your server:
```
External: 443/tcp → Internal: 192.168.1.124:443 (Traefik)
```

---

## Environment Variables

Edit `.env` file:

```bash
# API Key for internal service communication
API_KEY=your-secure-random-key-here

# YouTube Data API v3
# Get from: https://console.cloud.google.com/apis/credentials
YOUTUBE_API_KEY=AIzaSy...

# Google OAuth 2.0
# Get from: https://console.cloud.google.com/apis/credentials
GOOGLE_CLIENT_ID=123456789-abc123.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xyz789...

# SMTP Configuration (optional, for email notifications)
# Account: mgrabber@freaxnx01.ch
# Password from Passbolt
SMTP_PASSWORD=your-smtp-password

# Development (only used if OAuth not configured)
DEV_EMAIL=admin@example.com
DEV_NAME=Admin User
DEV_ROLE=Admin
```

---

## Deployment Steps

### Step 1: Prepare Environment

```bash
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen

# Create .env from template
cp .env.example .env

# Edit with your values
nano .env
```

### Step 2: Update Traefik Configuration

Add the mgrabber router to your Traefik dynamic configuration:

```yaml
# traefik/dynamic/mgrabber.yml
http:
  routers:
    mgrabber:
      rule: "Host(`mgrabber.home.freaxnx01.ch`)"
      entryPoints:
        - "websecure"
      tls:
        certResolver: "letsencrypt"
      service: "mgrabber"
  
  services:
    mgrabber:
      loadBalancer:
        servers:
          - url: "http://192.168.1.124:8086"
```

Or use Docker Compose labels (see Prerequisites).

### Step 3: Deploy

```bash
# Pull latest changes
git pull origin main

# Build and start
docker-compose down
docker-compose up -d --build

# Check logs
docker-compose logs -f
```

### Step 4: Verify

```bash
# Check containers are running
docker ps

# Check API health
curl http://192.168.1.124:8085/api/health

# Check frontend
curl -I https://mgrabber.home.freaxnx01.ch
```

---

## Post-Deployment

### 1. First Login

1. Visit: `https://mgrabber.home.freaxnx01.ch`
2. Click "Sign in with Google"
3. Authorize the app
4. You'll be redirected back to the app

### 2. Add Yourself to Whitelist

1. Go to: `https://mgrabber.home.freaxnx01.ch/admin/whitelist`
2. Click "Add User"
3. Enter your Google email
4. Check "Send welcome email" (optional)
5. Click "Add to List"

### 3. Test YouTube Search

1. Go to Home page
2. Search for a song/artist
3. Verify real YouTube results appear

---

## Troubleshooting

### Issue: "YouTube API Error"

**Cause:** Invalid or missing API key

**Fix:**
```bash
# Check .env file
nano .env

# Verify YOUTUBE_API_KEY is set
# Redeploy
docker-compose up -d --build
```

### Issue: "Authentication failed"

**Cause:** Google OAuth credentials incorrect

**Fix:**
1. Verify Client ID and Secret in .env
2. Check OAuth redirect URI in Google Console matches:
   `https://mgrabber.home.freaxnx01.ch/signin-google`
3. Ensure domain DNS is propagated:
   ```bash
   nslookup mgrabber.home.freaxnx01.ch
   ```

### Issue: "502 Bad Gateway" (Traefik)

**Cause:** Container not accessible or misconfigured

**Fix:**
```bash
# Check container is running
docker ps | grep mgrabber

# Check logs
docker logs mgrabber-frontend

# Verify network connectivity
docker network inspect traefik
```

### Issue: SSL Certificate Error

**Cause:** Let's Encrypt challenge failed

**Fix:**
1. Verify port 443 is forwarded correctly
2. Check Traefik logs:
   ```bash
   docker logs traefik
   ```
3. Ensure DNS A record is correct

---

## Maintenance

### Update Application

```bash
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen
git pull origin main
docker-compose down
docker-compose up -d --build
```

### Backup Database

```bash
# SQLite database is in Docker volume
docker cp mgrabber-download-api:/data/jobs.db ./backup-$(date +%Y%m%d).db
```

### Check Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f frontend
docker-compose logs -f download-api
```

### Monitor Resources

```bash
# Container stats
docker stats

# Disk usage
docker system df
```

---

## Security Checklist

- [ ] Changed default API_KEY
- [ ] Using HTTPS (not HTTP)
- [ ] Google OAuth credentials secured in Passbolt
- [ ] YouTube API key restricted to your domain
- [ ] .env file not committed to git
- [ ] SMTP password from Passbolt
- [ ] Firewall rules configured (only 443 open externally)

---

## Architecture

```
Internet
    ↓ HTTPS (443)
Router (Port Forward)
    ↓
Traefik (192.168.1.124:443)
    ↓
mgrabber-frontend (8086)
    ↓
mgrabber-download-api (8085)
    ↓
SQLite (/data/jobs.db)
```

---

## Support

- **GitHub Issues:** https://github.com/freaxnx01/mgrabber-nextgen/issues
- **Documentation:** See README.md
- **Logs:** `docker-compose logs -f`
