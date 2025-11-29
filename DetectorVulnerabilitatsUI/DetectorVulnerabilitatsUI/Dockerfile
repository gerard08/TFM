# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Copiem els fitxers de dependències primer (millor per a la caché de Docker)
COPY package*.json ./

# Usem 'npm ci' en lloc de 'install' perquè és més ràpid i fiable per a entorns nats (CI/CD/Docker)
RUN npm ci

# Copiem la resta del codi font
COPY . .

# Construïm l'aplicació en mode producció
RUN npm run build -- --configuration=production

# Stage 2: Serve amb Nginx
FROM nginx:alpine

# Netegem la carpeta per defecte de nginx
RUN rm -rf /usr/share/nginx/html/*

# --- PUNT CRÍTIC: RUTA DEL DIST ---
# En Angular 17/18/19, el build sovint genera: dist/NomDelProjecte/browser
# Si el teu projecte es diu 'DetectorVulnerabilitatsUI', verifica si existeix la carpeta 'browser'
# Opció A (Versions noves):
COPY --from=build /app/dist/DetectorVulnerabilitatsUI/browser /usr/share/nginx/html

# Opció B (Versions antigues o configuració clàssica):
# Si el build falla dient que no troba el directori, descomenta la línia de sota i comenta la de dalt:
# COPY --from=build /app/dist/DetectorVulnerabilitatsUI /usr/share/nginx/html

# Copiem la nostra configuració personalitzada de Nginx (creada al Pas 1)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
