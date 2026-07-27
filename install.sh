#!/bin/bash
set -e

echo "=== Fundament v0.1 — установка (без Docker) ==="

# Установка .NET 8
if ! command -v dotnet &> /dev/null; then
    echo "Устанавливаем .NET 8 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
    echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
    echo 'export PATH="$DOTNET_ROOT:$PATH"' >> ~/.bashrc
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

# Установка EF Core CLI
dotnet tool install --global dotnet-ef 2>/dev/null || true
export PATH="$HOME/.dotnet/tools:$PATH"

# Публикация
echo "Публикуем приложение..."
dotnet publish -c Release -o /opt/fundament

# Создание сервиса systemd
sudo tee /etc/systemd/system/fundament.service > /dev/null << 'SERVICE'
[Unit]
Description=Fundament — финансы строительства
After=network.target

[Service]
WorkingDirectory=/opt/fundament
ExecStart=/home/$USER/.dotnet/dotnet /opt/fundament/ConstructionFinance.dll --urls "http://0.0.0.0:5000"
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:5000

[Install]
WantedBy=multi-user.target
SERVICE

sudo systemctl daemon-reload
sudo systemctl enable fundament
sudo systemctl start fundament

echo ""
echo "=== Готово! ==="
echo "Откройте: http://$(curl -s ifconfig.me):5000"
echo "Статус: sudo systemctl status fundament"
echo "Логи: sudo journalctl -u fundament -f"
