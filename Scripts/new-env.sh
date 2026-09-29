#!/bin/sh
# Writes .env for docker compose from .env.example, with a fresh random value for every secret it
# leaves empty (the database, RabbitMQ, the token and service keys, the true administrator's
# password), so a fresh clone starts with "docker compose up" and no secret is ever shared.
# Needs openssl (Git for Windows, macOS and Linux have it). An existing .env is never overwritten.
#
#   Scripts/new-env.sh            then   docker compose up --build
set -eu

root=$(cd "$(dirname "$0")/.." && pwd)
example="$root/.env.example"
target="$root/.env"

if [ -e "$target" ]; then
  echo ".env exists already; delete it first to make a new one." >&2
  exit 1
fi

# 64 hexadecimal characters: long enough for every key, nothing to quote in .env or a connection string
key() { openssl rand -hex 32; }
# The identity service's password rules ask for an upper-case letter, a digit and a symbol
admin_password="Store-$(openssl rand -hex 8)-A1!"

cr=$(printf '\r')
while IFS= read -r line || [ -n "$line" ]; do
  # A Windows checkout may end the lines with CR LF
  line=${line%"$cr"}
  case "$line" in
    POSTGRES_PASSWORD= | RABBITMQ_PASSWORD= | JWT_SECRET_KEY= | INTERNAL_API_KEY= | PAYMENT_WEBHOOK_SECRET=)
      printf '%s%s\n' "$line" "$(key)" ;;
    TRUE_ADMIN_PASSWORD=)
      printf '%s%s\n' "$line" "$admin_password" ;;
    *)
      printf '%s\n' "$line" ;;
  esac
done < "$example" > "$target"

echo "Wrote $target with fresh secrets."
echo "The true administrator signs in as $(grep '^TRUE_ADMIN_EMAIL=' "$target" | cut -d= -f2-) with the password in TRUE_ADMIN_PASSWORD there."
