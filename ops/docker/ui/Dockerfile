FROM nginx:stable-alpine
LABEL org.opencontainers.image.source="https://github.com/dialden/resume-life-simplifier"
RUN apk add --no-cache jq
COPY . /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY configure-ui.sh /docker-entrypoint.d/40-configure-ui.sh
RUN chmod +x /docker-entrypoint.d/40-configure-ui.sh \
    && rm /usr/share/nginx/html/nginx.conf /usr/share/nginx/html/configure-ui.sh
EXPOSE 80
