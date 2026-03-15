#!/bin/bash
# Test SMTP configuration script
# Usage: ./test-email.sh <recipient-email> [smtp-password]

RECIPIENT="${1:-mgrabber@freaxnx01.ch}"
SMTP_PASS="${2:-}"

if [ -z "$SMTP_PASS" ]; then
    echo "Usage: $0 <recipient-email> <smtp-password>"
    echo "Example: $0 mgrabber@freaxnx01.ch 'your-password-from-passbolt'"
    exit 1
fi

echo "Testing SMTP configuration..."
echo "From: mgrabber@freaxnx01.ch"
echo "To: $RECIPIENT"
echo "Server: mail.freaxwave.ch:587"
echo ""

# Create test email content
cat > /tmp/test-email.txt << 'EOF'
Subject: 🎵 Music Grabber - SMTP Test
From: mgrabber@freaxnx01.ch
To: RECIPIENT
Content-Type: text/html; charset=UTF-8

<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #6366f1, #8b5cf6); color: white; padding: 30px; text-align: center; border-radius: 8px; }
        .content { background: #f9fafb; padding: 30px; border-radius: 8px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎵 SMTP Test Successful!</h1>
        </div>
        <div class='content'>
            <p>This is a test email from Music Grabber.</p>
            <p>If you're seeing this, the SMTP configuration is working correctly!</p>
            <p><strong>Configuration:</strong></p>
            <ul>
                <li>Server: mail.freaxwave.ch:587</li>
                <li>From: mgrabber@freaxnx01.ch</li>
                <li>To: RECIPIENT</li>
                <li>Time: DATE</li>
            </ul>
        </div>
    </div>
</body>
</html>
EOF

# Replace placeholders
sed -i "s/RECIPIENT/$RECIPIENT/g" /tmp/test-email.txt
sed -i "s/DATE/$(date)/g" /tmp/test-email.txt

# Send email using swaks (if available) or curl
if command -v swaks &> /dev/null; then
    echo "Using swaks to send email..."
    swaks --to "$RECIPIENT" \
          --from "mgrabber@freaxnx01.ch" \
          --server mail.freaxwave.ch:587 \
          --auth-user "mgrabber@freaxnx01.ch" \
          --auth-password "$SMTP_PASS" \
          --tls \
          --data /tmp/test-email.txt
elif command -v curl &> /dev/null; then
    echo "Using curl to send email (simplified)..."
    echo "Note: For full SMTP with curl, additional setup needed"
    echo ""
    echo "Alternative: Use the application itself to test:"
    echo "1. Deploy the application"
    echo "2. Add a user to whitelist with 'Send welcome email' checked"
    echo "3. Check if email arrives"
else
    echo "Neither swaks nor appropriate curl available."
    echo ""
    echo "To test manually:"
    echo "1. Deploy the application with: docker-compose up -d --build"
    echo "2. Set SMTP_PASSWORD in .env file"
    echo "3. Visit http://192.168.1.124:8086/admin/whitelist"
    echo "4. Add a user and check 'Send welcome email'"
    echo "5. Check recipient inbox"
fi

# Cleanup
rm -f /tmp/test-email.txt

echo ""
echo "Test complete!"
