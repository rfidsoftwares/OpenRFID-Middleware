#!/bin/bash
# OpenRFID Middleware - Linux Systemd Installer Script
# Run with sudo

set -e

echo "=================================================="
echo "📡 Installing OpenRFID Middleware Systemd Service"
echo "=================================================="

if [ "$EUID" -ne 0 ]; then
  echo "Error: Please run as root (sudo)."
  exit 1
fi

INSTALL_DIR="/opt/openrfid"
SERVICE_FILE="/etc/systemd/system/openrfid.service"

echo "Creating installation directory $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"

echo "Copying systemd service unit..."
cp "$(dirname "$0")/openrfid.service" "$SERVICE_FILE"

echo "Reloading systemd daemon..."
systemctl daemon-reload
systemctl enable openrfid.service

echo "✅ OpenRFID Middleware Service installed successfully!"
echo "To start: sudo systemctl start openrfid"
echo "To check status: sudo systemctl status openrfid"
