#!/bin/bash
set -e

echo "=== Fundament v0.1 — установка ==="

# Проверка наличия Docker
if ! command -v docker &> /dev/null; then
    echo "Устанавливаем Docker..."
    curl -fsSL https://get.docker.com | bash
    sudo usermod -aG docker $USER
    echo "Docker установлен. Перезайдите в систему и запустите скрипт снова."
    exit 0
fi

# Сборка и запуск
echo "Собираем образ..."
docker compose build

echo "Запускаем..."
docker compose up -d

echo ""
echo "=== Готово! ==="
echo "Откройте: http://localhost:5000"
echo "Остановить: docker compose down"
echo "Логи: docker compose logs -f"
