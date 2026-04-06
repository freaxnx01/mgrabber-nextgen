# Developer Notes

## SSH Port Forwarding

To access the app running on the remote dev server from your local machine:

```bash
ssh -N -L 8086:localhost:8086 freax@192.168.1.108
```

Then open `http://localhost:8086` in your browser.
