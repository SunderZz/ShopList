# Specs fonctionnelles prioritaires

Ce document décrit les comportements attendus avant implémentation.

## Gestion des utilisateurs et droits

### Rôles

- Utilisateur standard: accès à ses propres listes uniquement.
- Administrateur: accès global aux utilisateurs, ingrédients, plats et listes.

### Règles

- Un utilisateur standard ne peut jamais modifier son rôle.
- Un utilisateur standard ne peut pas créer un autre utilisateur admin.
- L'inscription publique crée toujours un utilisateur standard.
- La création d'un administrateur doit être réservée à un administrateur existant ou à une procédure de bootstrap contrôlée.

## Listes de courses

### Visibilité

- Un utilisateur standard voit uniquement les listes dont `ownerId` correspond à son id.
- Un administrateur peut voir toutes les listes.
- Un utilisateur anonyme ne voit aucune liste.

### Actions

- Un utilisateur standard peut créer, consulter, modifier, supprimer et cocher les items de ses listes.
- Un utilisateur standard ne peut pas accéder à une liste d'un autre utilisateur, même avec l'id direct.
- Un administrateur peut gérer toutes les listes.

### Affichage

- Le détail d'une liste doit afficher les plats sélectionnés pour générer cette liste.
- L'utilisateur doit pouvoir distinguer les plats sélectionnés des ingrédients ajoutés manuellement.

### Données

- `items` représente les lignes matérialisées et agrégées affichées à l'utilisateur.
- `manualItems` représente les ingrédients ajoutés hors plats et doit être conservé séparément pour éviter les doubles comptages lors de l'édition.
- Une ligne peut exposer `quantities` si plusieurs quantités explicites doivent être affichées pour le même ingrédient.

## Plats

### Unicité

- Deux plats ne doivent pas représenter la même recette.
- La comparaison doit ignorer les différences évidentes: espaces en trop, casse, accents si la règle est validée.

### Champ lien

- Un plat peut contenir un lien optionnel vers une recette externe.
- Le champ recommandé est `sourceUrl`.
- Les URLs acceptées doivent être HTTP ou HTTPS.
- Les URLs contenant des identifiants intégrés sont refusées.
- Exemples de sources prévues: YouTube, Marmiton, TikTok, blogs de cuisine.

### Affichage

- Si un lien est présent, le front affiche une action permettant d'ouvrir la source.
- Le lien doit s'ouvrir dans un nouvel onglet avec protection `noopener noreferrer`.

## Ingrédients

### Unicité

- Deux ingrédients ne doivent pas représenter le même ingrédient.
- La comparaison doit utiliser un nom normalisé.
- Exemple: `Tomate`, ` tomate ` et `TOMATE` doivent être considérés comme le même ingrédient.

### Rayons

- Le champ rayon reste optionnel.
- Les rayons doivent être conservés dans les listes pour faciliter les courses.

## Unités de mesure

### Problème actuel

Quand deux plats contiennent le même ingrédient avec deux unités différentes, la quantité peut devenir invisible ou ambiguë.

### Comportement attendu

- Les unités compatibles doivent être converties et additionnées.
- Les unités incompatibles doivent rester explicites au lieu de faire disparaître l'information.

### Unités à normaliser

Première proposition:

- Masse: `g`, `kg`
- Volume: `ml`, `cl`, `l`
- Pièce: `unité`
- Conditionnement: `paquet`

### Règles de conversion proposées

- `1 kg = 1000 g`
- `1 l = 1000 ml`
- `1 cl = 10 ml`
- `unité` ne se convertit pas vers masse ou volume.
- `paquet` ne se convertit pas sans metadata additionnelle.

### Décision implémentée

- Les masses sont matérialisées en `g`.
- Les volumes sont matérialisés en `ml`.
- `paquet` et `unité` restent des unités distinctes.
- Les familles incompatibles restent visibles via plusieurs quantités explicites.

### Rendu attendu

- Si unités compatibles: afficher une quantité agrégée.
- Si unités incompatibles: afficher plusieurs lignes ou une indication claire, par exemple `2 unités + 300 g`.

## Mobile

### Exigence générale

La version mobile doit permettre les mêmes actions essentielles que desktop.

### Écrans à vérifier

- Accueil et login/register.
- Liste des ingrédients.
- Liste des plats.
- Création et édition de plat.
- Liste des listes de courses.
- Détail d'une liste.
- Gestion utilisateurs pour admin.

### Critères UX

- Les boutons doivent être accessibles au doigt.
- Les modales doivent tenir dans l'écran et scroller correctement.
- Les tableaux doivent avoir une alternative mobile lisible.
- Le texte ne doit pas déborder.
- Les messages d'erreur doivent être visibles sans ouvrir la console.
