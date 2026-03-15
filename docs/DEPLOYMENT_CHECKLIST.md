# Production Deployment Checklist

## ✅ Pre-Deployment Checklist

### 1. Google Cloud Configuration
- [x] YouTube Data API v3 enabled
- [x] YouTube API Key created and restricted
- [x] Google OAuth 2.0 credentials created
- [x] OAuth redirect URI configured: `https://mgrabber.home.freaxnx01.ch/signin-google`

### 2. DNS Configuration
- [ ] Create A record for `mgrabber.home.freaxnx01.ch`
  - Type: A
  - Name: mgrabber.home
  - Value: YOUR_EXTERNAL_IP
  - TTL: 300
- [ ] Verify DNS propagation: `nslookup mgrabber.home.freaxnx01.ch`

### 3. Router Configuration
- [ ] Port forward 443/TCP → 192.168.1.124:443 (Traefik)
- [ ] Port forward 80/TCP → 192.168.1.124:80 (Traefik redirect)

### 4. Environment Variables
- [ ] `.env` file created from `.env.example`
- [ ] `YOUTUBE_API_KEY` set
- [ ] `GOOGLE_CLIENT_ID` set
- [ ] `GOOGLE_CLIENT_SECRET` set
- [ ] `API_KEY` set (secure random string)
- [ ] `SMTP_PASSWORD` set (optional)

### 5. Traefik Setup
- [ ] Traefik running with Docker
- [ ] Traefik network created: `docker network create traefik`
- [ ] Let's Encrypt configured in Traefik
- [ ] Traefik dashboard accessible (optional)

## 🚀 Deployment Steps

### Step 1: SSH to Server
```bash
ssh ubuntu@192.168.1.124
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen
```

### Step 2: Run Deployment Script
```bash
./deploy.sh
```

Or manually:
```bash
# Pull latest
git pull origin main

# Create/update .env
cp .env.example .env
nano .env  # Add your credentials

# Deploy
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### Step 3: Verify Deployment
```bash
# Check containers
docker ps

# Check API health
curl http://localhost:8085/api/health

# Check Traefik routes
docker logs traefik | grep mgrabber
```

## 🔍 Post-Deployment Verification

### 1. SSL Certificate
- [ ] Visit `https://mgrabber.home.freaxnx01.ch`
- [ ] Certificate is valid (green lock icon)
- [ ] No certificate warnings

### 2. Application Functionality
- [ ] Homepage loads
- [ ] MusicBrainz search works
- [ ] YouTube search works
- [ ] Google OAuth login works
- [ ] Download starts successfully
- [ ] Admin pages accessible (after login)

### 3. OAuth Flow
- [ ] Click "Sign in with Google"
- [ ] Authorize the app
- [ ] Redirect back to app successful
- [ ] User shows as logged in

### 4. End-to-End Test
- [ ] Search for artist on MusicBrainz
- [ ] Click YouTube button
- [ ] Download a video
- [ ] Verify file appears in downloads

## 🔧 Troubleshooting

### Issue: SSL Certificate Error
**Symptoms:** Certificate not valid, browser warning
**Solutions:**
1. Wait 5-10 minutes for Let's Encrypt
2. Check Traefik logs: `docker logs traefik`
3. Verify DNS: `nslookup mgrabber.home.freaxnx01.ch`
4. Check port 443 is forwarded

### Issue: OAuth Login Fails
**Symptoms:** "Authentication failed" or redirect error
**Solutions:**
1. Verify OAuth redirect URI in Google Console
2. Check `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` in .env
3. Ensure HTTPS is working
4. Check domain matches exactly

### Issue: YouTube Search Fails
**Symptoms:** "YouTube API Error"
**Solutions:**
1. Verify `YOUTUBE_API_KEY` in .env
2. Check API key restrictions allow your domain
3. Check YouTube API quota not exceeded

### Issue: 502 Bad Gateway
**Symptoms:** "Bad Gateway" error from Traefik
**Solutions:**
1. Check containers running: `docker ps`
2. Check frontend logs: `docker-compose logs frontend`
3. Verify Traefik network: `docker network inspect traefik`
4. Restart: `docker-compose restart`

## 📋 Final Status

Once all items are checked:

**Update GitHub Issue #6:**
```bash
# Using GitHub CLI (if available)
gh issue close 6 --comment "Deployed successfully to https://mgrabber.home.freaxnx01.ch"

# Or via API
curl -X PATCH \
  -H "Authorization: token $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/freaxnx01/mgrabber-nextgen/issues/6 \
  -d '{"state":"closed","body":"Deployed to production\n\n- Domain: https://mgrabber.home.freaxnx01.ch\n- SSL: Let's Encrypt\n- Traefik: Configured\n- OAuth: Working\n- All features functional"}'
```

## 🎉 Success Criteria

Deployment is **COMPLETE** when:
- ✅ HTTPS works without warnings
- ✅ Google OAuth login works
- ✅ YouTube search returns results
- ✅ MusicBrainz search works
- ✅ Downloads complete successfully
- ✅ All admin features work

**Estimated time:** 30-60 minutes
