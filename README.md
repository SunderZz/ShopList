# ShopList

Application personnelle de liste de courses: l'utilisateur sélectionne des plats, ajoute éventuellement des ingrédients manuels, puis obtient une liste agrégée par rayon.

- Front production: https://shop-list-two.vercel.app/
- API production: https://shoplist-33qz.onrender.com/

## Stack actuelle

- Back-end: ASP.NET Core .NET 8, MongoDB, JWT Bearer, FluentValidation, rate limiting, Swagger en développement.
- Front-end: Vue 3, Vite, Pinia, Vue Router, Axios, Tailwind CSS.
- Déploiement: Vercel pour le front, Render pour l'API.

## Fonctionnalités principales

- Authentification JWT avec rôles utilisateur standard et administrateur.
- Isolation des listes par propriétaire: un utilisateur standard ne voit que ses listes.
- Gestion des ingrédients et des plats avec normalisation des noms pour limiter les doublons.
- Génération de listes à partir de plats sélectionnés.
- Ajout d'ingrédients manuels hors plats.
- Normalisation des unités pour agréger correctement les quantités compatibles.

## Normalisation des unités

Le catalogue partagé back/front contient:

- Masse: `g`, `kg`, agrégées en `g`.
- Volume: `ml`, `cl`, `l`, agrégés en `ml`.
- Pièce: `unité`.
- Conditionnement: `paquet`.

Quand un ingrédient arrive avec plusieurs familles d'unités incompatibles, la liste conserve plusieurs quantités explicites, par exemple `2 unité + 300 g`, au lieu de masquer l'information.

Les listes stockent désormais les ingrédients manuels dans `manualItems` et les lignes matérialisées dans `items`. Chaque ligne peut exposer `quantities` pour représenter plusieurs quantités explicites.

## Structure utile

```text
Back/
  ListeDeCourses/Api/              API ASP.NET Core
  ListeDeCourses.Api.Tests/        Tests métier exécutables
Front/
  listedecourses-front/            Application Vue/Vite
docs/
  00-CODEX_CONTEXT.md              Règles et contexte de travail
  01-PLAN_ACTION.md                Avancement par sprint
  02-SPECS_FONCTIONNELLES.md       Comportements attendus
  03-BACKLOG_IDEES.md              Idées non prioritaires
```

## Prérequis locaux

- .NET SDK 8.
- Node.js et npm.
- MongoDB local, ou une URI MongoDB accessible.

## Variables d'environnement

### API

- `MONGODB_URI`: URI MongoDB. Si l'URI contient un nom de base, il est utilisé.
- `MongoDbDatabase`: nom de base utilisé si `MONGODB_URI` ne contient pas de base.
- `JWT__KEY`: secret JWT, requis hors développement.
- `CORS_ALLOWED_ORIGINS` ou `FRONTEND_URL`: origines front autorisées hors développement.

### Front

- `VITE_API_BASE_URL`: URL de base API, par exemple `http://localhost:5145/api`.
- `VITE_HMR_HOST`: optionnel, utile dans certains environnements distants.

## Commandes locales

### API

```sh
cd Back/ListeDeCourses/Api
dotnet restore ApiList.csproj
dotnet run
```

En développement, l'API utilise `mongodb://localhost:27017`, la base `ShopList`, et écoute en local sur `http://localhost:5145`.

### Tests back

```sh
dotnet run --project Back/ListeDeCourses.Api.Tests/Api.Tests.csproj --no-restore
```

### Front

```sh
cd Front/listedecourses-front
npm install
npm run dev
npm run typecheck
npm run lint
npm run build
```

En développement, Vite proxie les appels `/api` vers `http://localhost:5145`.

## Vérifications recommandées avant push

```sh
dotnet run --project Back/ListeDeCourses.Api.Tests/Api.Tests.csproj --no-restore
cd Front/listedecourses-front
npm run typecheck
npm run build
```

`npm run lint` reste recommandé quand le temps le permet. `npm audit --omit=dev` vérifie les vulnérabilités runtime front.
