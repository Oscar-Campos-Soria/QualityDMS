FROM nginx:alpine

RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/ssl \
    && openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
       -keyout /etc/nginx/ssl/privkey.pem \
       -out    /etc/nginx/ssl/fullchain.pem \
       -subj "/C=MX/ST=Jalisco/L=Guadalajara/O=DMS/CN=localhost" \
    && chmod 600 /etc/nginx/ssl/privkey.pem

COPY nginx.conf /etc/nginx/nginx.conf
