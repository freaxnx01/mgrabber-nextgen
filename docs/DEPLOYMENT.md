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
docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# 5. Verify
# App:          https://mgrabber.home.freaxnx01.ch
# Health (live): http://192.168.1.124:8086/health/live
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

Traefik is configured via the file provider — copy [`traefik/mgrabber.yml`](../traefik/mgrabber.yml) into your
Traefik dynamic config directory (see Step 2 in Deployment Steps below). It defines:

- HTTPS router with Let's Encrypt (`certResolver: letsencrypt`) and a hardened TLS options set (TLS 1.2+, strong
  cipher suites) for an SSL Labs A+ rating
- HTTP → HTTPS redirect router
- HSTS response header
- WebSocket support (Traefik proxies `Upgrade`/`Connection` headers automatically — no extra config needed; used by
  the SignalR download-progress hub at `/hubs/download`)

The backend target (`http://192.168.1.124:8086`) must match the port published by `docker-compose.prod.yml`.

### 4. Router Port Forwarding

Forward external ports to your Traefik host:
```
External: 443/tcp → Internal: 192.168.1.124:443 (Traefik, HTTPS)
External: 80/tcp  → Internal: 192.168.1.124:80  (Traefik, HTTP→HTTPS redirect)
```

---

## Environment Variables

Edit `.env` file (see [`.env.example`](../.env.example) for the full list):

```bash
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

Copy [`traefik/mgrabber.yml`](../traefik/mgrabber.yml) from this repo into your Traefik dynamic config directory
(e.g. `traefik/dynamic/mgrabber.yml` on the Traefik host).

### Step 3: Deploy

```bash
# Pull latest changes
git pull origin main

# Build and start (production port mapping for Traefik)
docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Check logs
docker-compose logs -f
```

### Step 4: Verify

```bash
# Check container is running
docker ps

# Check health
curl http://192.168.1.124:8086/health/live

# Check app over Traefik
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
# Check container is running and the prod port mapping (8086:8080) is up
docker ps | grep musicgrabber

# Check logs
docker logs musicgrabber

# Verify the backend is reachable from the Traefik host
curl http://192.168.1.124:8086/health/live
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
docker-compose -f docker-compose.yml -f docker-compose.prod.yml down
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### Backup Database

```bash
# SQLite database is in the 'data' Docker volume
docker cp musicgrabber:/data/musicgrabber.db ./backup-$(date +%Y%m%d).db
```

### Check Logs

```bash
docker-compose logs -f musicgrabber
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
musicgrabber container (host port 8086 → container port 8080)
    ↓
SQLite (/data/musicgrabber.db)
```

---

## Support

- **GitHub Issues:** https://github.com/freaxnx01/mgrabber-nextgen/issues
- **Documentation:** See README.md
- **Logs:** `docker-compose logs -f`
