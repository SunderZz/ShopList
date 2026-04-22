# Backlog idées futures

Ces idées sont à garder pour la fin, après correction sécurité, bugs et dette technique prioritaire.

## Récupération d'ingrédients depuis un lien

### Idée

Depuis le lien d'un plat, tenter de récupérer automatiquement les ingrédients de la recette.

### Sources possibles

- Pages de recettes avec données structurées `schema.org/Recipe`.
- Marmiton ou sites similaires.
- Vidéos YouTube ou TikTok si une description contient les ingrédients.

### Points à étudier

- Respect des conditions d'utilisation des sites.
- Gestion des pages dynamiques.
- Qualité variable des données récupérées.
- Normalisation des ingrédients extraits.
- Mapping vers les ingrédients existants pour éviter les doublons.
- Validation manuelle obligatoire avant sauvegarde.

### Proposition de workflow

1. L'utilisateur colle un lien dans un plat.
2. Le back récupère les données disponibles.
3. Le système propose une liste d'ingrédients.
4. L'utilisateur corrige, fusionne et valide.
5. Les ingrédients sont ajoutés au plat.

## Étapes de cuisine

### Idée

Ajouter les étapes de préparation à un plat pour transformer l'application en assistant repas léger.

### Données possibles

- `steps`: liste ordonnée de textes.
- `preparationTimeMinutes`.
- `cookingTimeMinutes`.
- `servings`.

### Points à étudier

- Édition ergonomique sur mobile.
- Réorganisation des étapes.
- Import éventuel depuis une recette externe.
- Mode lecture pendant la cuisine.

## Suggestions de plats

### Idée

Proposer des plats à partir des ingrédients déjà disponibles ou souvent utilisés.

### Points à étudier

- Score basé sur ingrédients communs.
- Historique des listes.
- Préférences utilisateur.
- Exclusion des plats déjà planifiés.

## Partage de listes

### Idée

Permettre à plusieurs utilisateurs de collaborer sur une même liste.

### Points à étudier

- Droits owner/editor/viewer.
- Invitations.
- Conflits de modification.
- Synchronisation temps réel ou polling.
