# Front ShopList

Application Vue 3/Vite pour gérer les ingrédients, les plats et les listes de courses.

## Stack

- Vue 3 avec Composition API.
- Vite.
- Pinia pour l'état applicatif.
- Vue Router.
- Axios pour l'API.
- Tailwind CSS pour le style.

## Configuration

Créer un fichier `.env.local` si l'API locale ne doit pas passer par le proxy Vite:

```sh
VITE_API_BASE_URL=http://localhost:5145/api
```

En mode développement, le proxy Vite redirige `/api` vers `http://localhost:5145`.

## Commandes

```sh
npm install
npm run dev
npm run typecheck
npm run lint
npm run build
```

## Contrat API utilisé par le front

Les listes de courses exposent:

- `items`: lignes agrégées et affichées dans le détail de liste.
- `manualItems`: ingrédients ajoutés manuellement hors plats, utilisés pour rouvrir l'éditeur sans double comptage.
- `quantities`: détail optionnel des quantités explicites quand plusieurs familles d'unités coexistent.

Le front partage le même catalogue d'unités que le back via `src/utils/units.ts`: `g`, `kg`, `ml`, `cl`, `l`, `paquet`, `unité`.

## Points de vigilance UI

- Les vues listes doivent rester utilisables sur mobile.
- Les ingrédients manuels demandent toujours une quantité positive et une unité.
- Le détail de liste affiche une synthèse comme `1500 g` ou `2 unité + 300 g` selon l'agrégation fournie par l'API.
