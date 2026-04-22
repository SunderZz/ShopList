# Plan d'action

Ce document découpe le travail en jalons courts. Chaque sprint doit produire une application utilisable, avec vérification front, back et mobile.

## Avancement global

Avancement: `19/55 tâches - 35%`

Une tâche cochée compte comme terminée uniquement si elle est implémentée, relue et vérifiée. Les critères d'acceptation servent à valider le sprint, mais ne sont pas comptés comme tâches.

## Sprint 0 - Stabilisation projet

Objectif: rendre le projet contrôlable avant les changements métier.

Avancement: `7/7 tâches - 100%`

### Tâches

- [x] Ajouter un `.gitignore` adapté à .NET, Node/Vite, Vercel et fichiers locaux.
- [x] Retirer du suivi Git les artefacts générés comme `Back/ListeDeCourses/Api/out/`.
- [x] Vérifier que les secrets de développement ne sont pas utilisés en production.
- [x] Réparer `npm run typecheck`.
- [x] Réparer `npm run lint` ou aligner le script avec les dépendances installées.
- [x] Mettre à jour les dépendances front vulnérables, notamment `axios` et `follow-redirects`.
- [x] Documenter les commandes locales fiables pour lancer front et back.

### Notes de vérification

- `npm run typecheck`, `npm run lint`, `npm run build` et `dotnet build ApiList.csproj` passent.
- `npm audit --omit=dev --audit-level=moderate` ne signale plus de vulnérabilité runtime.
- Le full audit npm conserve l'alerte dev-server `vite/esbuild`; sa correction demande une migration majeure de Vite et reste à traiter dans une passe dédiée.

### Critères d'acceptation

- `git status` ne contient pas d'artefacts générés inattendus.
- Le typecheck front fonctionne.
- Le lint front fonctionne ou une décision documentée remplace temporairement cette étape.
- L'audit npm ne signale plus de vulnérabilité runtime connue.

## Sprint 1 - Sécurité API et authentification

Objectif: fermer les failles critiques d'accès.

Avancement: `9/9 tâches - 100%`

### Tâches

- [x] Protéger les contrôleurs CRUD par défaut avec `[Authorize]`.
- [x] Garder uniquement les routes publiques nécessaires: santé, login, register.
- [x] Séparer l'inscription publique de la création admin d'utilisateur.
- [x] Empêcher un client public d'envoyer ou modifier `isSuperUser`.
- [x] Ajouter des policies admin pour les routes utilisateurs et les actions globales.
- [x] Refuser explicitement les appels anonymes dans les services sensibles.
- [x] Restreindre CORS aux origines configurées, sans `AllowAnyOrigin` en production.
- [x] Aligner le contrat login front/back.
- [x] Ajouter une protection contre brute force ou au minimum un rate limit sur login/register.

### Notes de vérification

- `dotnet build ApiList.csproj -o obj/codex-build`, `npm run typecheck` et `npm run lint` passent.
- Smoke test local: `/healthz` répond `200`, `/ingredients` et `/utilisateurs` répondent `401` en anonyme.
- `dotnet build ApiList.csproj` vers `bin/` peut échouer si l'API locale est déjà lancée et verrouille `ApiList.dll`/`ApiList.exe`; arrêter le serveur avant un build normal.

### Critères d'acceptation

- Un appel anonyme aux ressources métier reçoit `401`.
- Un utilisateur standard ne peut pas créer, lister, modifier ou supprimer les utilisateurs.
- Un utilisateur standard ne peut jamais se promouvoir admin.
- Un admin garde les droits nécessaires.
- Le front sait se connecter sans comportement de rattrapage fragile.

## Sprint 2 - Isolation des données utilisateur

Objectif: garantir que chaque utilisateur standard ne voit que ses listes.

Avancement: `3/5 tâches - 60%`

### Tâches

- [x] Filtrer les listes par `ownerId` directement au niveau MongoDB.
- [x] Permettre à un admin de voir toutes les listes.
- [x] Vérifier les droits sur lecture, modification, suppression et cochage d'item.
- [ ] Ajouter des tests back pour les scénarios owner/admin/anonyme.
- [ ] Adapter le front si nécessaire pour les vues admin.

### Notes de vérification

- `ListeService.GetAllAsync` utilise maintenant `ListeRepository.GetByOwnerIdAsync` pour les utilisateurs standard.
- Compatibilité données existantes: la requête accepte `ownerId` et l'ancien champ `userId` sans migration destructive.
- Les admins gardent `GetAllAsync` global.
- Les accès par id, modification, suppression et cochage passent par `EnsureOwnership`.

### Critères d'acceptation

- Un utilisateur A ne voit jamais les listes de l'utilisateur B.
- Un utilisateur A ne peut pas deviner un `id` de liste et accéder à celle de B.
- Un admin peut lister et consulter toutes les listes.
- Les tests couvrent ces cas.

## Sprint 3 - Unicité plats et ingrédients

Objectif: éviter les doublons métier.

Avancement: `5/6 tâches - 83%`

### Tâches

- [x] Définir la règle d'unicité pour les ingrédients: `name` trim, casse ignorée, espaces multiples normalisés.
- [x] Définir la règle d'unicité pour les plats: `name` trim, casse ignorée, espaces multiples normalisés.
- [ ] Ajouter des index uniques MongoDB.
- [x] Normaliser côté back les noms: trim, casse, espaces multiples.
- [x] Retourner `409 Conflict` en cas de doublon.
- [x] Adapter le front pour afficher un message clair.

### Notes de vérification

- L'API bloque les nouveaux doublons sans migration destructive.
- Les nouveaux documents reçoivent un champ `nameKey` préparant les futurs index.
- Les index uniques restent volontairement en attente: ils peuvent échouer si la base MongoDB contient déjà des doublons.
- Avant d'activer ces index, auditer les collections `ingredients` et `plats` sur la clé normalisée.

### Critères d'acceptation

- Impossible de créer deux ingrédients équivalents comme `Tomate`, ` tomate ` ou `TOMATE`.
- Impossible de créer deux plats équivalents.
- Les erreurs de doublon sont lisibles côté UI.

## Sprint 4 - Normalisation des unités et agrégation

Objectif: additionner correctement les ingrédients communs même si les plats utilisent des unités compatibles différentes.

Avancement: `0/6 tâches - 0%`

### Tâches

- [ ] Définir un modèle d'unités canonique.
- [ ] Ajouter une table ou un service de conversion pour les unités compatibles.
- [ ] Convertir les quantités vers une unité canonique avant agrégation.
- [ ] Gérer les cas non convertibles avec une règle explicite.
- [ ] Aligner front et back sur la même liste d'unités.
- [ ] Ajouter des tests sur les conversions et l'agrégation de listes.

### Critères d'acceptation

- `500 g` + `1 kg` du même ingrédient devient `1.5 kg` ou `1500 g` selon la règle choisie.
- Les quantités ne disparaissent plus quand les unités sont compatibles.
- Les unités non compatibles restent visibles avec une indication claire.

## Sprint 5 - Lien recette sur les plats

Objectif: associer un lien externe à un plat.

Avancement: `0/5 tâches - 0%`

### Tâches

- [ ] Ajouter un champ optionnel `recipeUrl` ou `sourceUrl` sur les plats.
- [ ] Valider que le lien est une URL HTTP/HTTPS.
- [ ] Afficher le lien dans la liste et le détail du plat.
- [ ] Ouvrir le lien dans un nouvel onglet avec `rel="noopener noreferrer"`.
- [ ] Préparer ce champ pour les futures fonctionnalités de récupération d'ingrédients.

### Critères d'acceptation

- Un plat peut être créé et modifié avec ou sans lien.
- Les liens YouTube, Marmiton, TikTok et sites classiques sont acceptés si URL valides.
- Les URLs invalides sont rejetées côté back et expliquées côté front.

## Sprint 6 - Qualité front et mobile

Objectif: rendre l'interface plus fiable et confortable sur desktop et mobile.

Avancement: `0/6 tâches - 0%`

### Tâches

- [ ] Tester chaque écran sur mobile: accueil, login/register, ingrédients, plats, listes, détail liste, utilisateurs admin.
- [ ] Corriger les débordements, boutons trop petits, modales difficiles à utiliser.
- [ ] Supprimer le blocage du zoom mobile sauf justification forte.
- [ ] Améliorer les états loading, empty, error.
- [ ] Ajouter confirmations cohérentes sur suppressions sensibles.
- [ ] Réduire les SVG dupliqués ou introduire une petite couche d'icônes réutilisables.

### Critères d'acceptation

- Toutes les fonctions principales sont utilisables sur écran mobile.
- Aucun texte ou bouton important ne déborde.
- Les erreurs API sont visibles et compréhensibles.
- Le zoom navigateur n'est pas bloqué sans raison d'accessibilité documentée.

## Sprint 7 - Dette back et performance

Objectif: solidifier les fondations sans changer inutilement le produit.

Avancement: `0/6 tâches - 0%`

### Tâches

- [ ] Remplacer les `GetAllAsync()` puis filtrages mémoire par des queries Mongo ciblées.
- [ ] Introduire pagination ou recherche pour les collections qui peuvent grossir.
- [ ] Réduire les N+1 queries lors de l'agrégation des listes.
- [ ] Clarifier les responsabilités repositories/services.
- [ ] Unifier les réponses d'erreur API.
- [ ] Ajouter tests unitaires et tests d'intégration sur les règles métier.

### Critères d'acceptation

- Les endpoints principaux ne chargent pas toute la base sans besoin.
- Les règles critiques ont des tests.
- Les erreurs API sont cohérentes.

## Sprint 8 - Documentation et specs durables

Objectif: rendre le projet facile à reprendre.

Avancement: `0/5 tâches - 0%`

### Tâches

- [ ] Mettre à jour le README avec stack réelle, commandes, variables d'environnement.
- [ ] Documenter les routes API.
- [ ] Documenter les modèles MongoDB actuels.
- [ ] Mettre à jour les specs après chaque décision métier.
- [ ] Tenir un backlog clair des tâches restantes.

### Critères d'acceptation

- Un nouveau contributeur peut lancer le projet localement.
- Les choix métier importants sont documentés.
- Les prochaines tâches sont identifiables sans refaire toute la review.
