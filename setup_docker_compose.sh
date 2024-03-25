sudo mkdir -p /websmtp/certificates/
sudo openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 \
  -nodes -keyout /websmtp/certificates/ssl.key -out /websmtp/certificates/ssl.crt -subj "/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,DNS:*.localhost"