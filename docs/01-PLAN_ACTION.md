# Plan d'action

Ce document découpe le travail en jalons courts. Chaque sprint doit produire une application utilisable, avec vérification front, back et mobile.

## Avancement global

Avancement: `42/62 tâches - 68%`

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

Avancement: `5/5 tâches - 100%`

### Tâches

- [x] Filtrer les listes par `ownerId` directement au niveau MongoDB.
- [x] Permettre à un admin de voir toutes les listes.
- [x] Vérifier les droits sur lecture, modification, suppression et cochage d'item.
- [x] Ajouter des tests back pour les scénarios owner/admin/anonyme.
- [x] Adapter le front si nécessaire pour les vues admin.

### Notes de vérification

- `ListeService.GetAllAsync` utilise maintenant `ListeRepository.GetByOwnerIdAsync` pour les utilisateurs standard.
- Compatibilité données existantes: la requête accepte `ownerId` et l'ancien champ `userId` sans migration destructive.
- Les admins gardent `GetAllAsync` global.
- Les accès par id, modification, suppression et cochage passent par `EnsureOwnership`.
- Tests back ajoutes dans `Back/ListeDeCourses.Api.Tests`: anonyme, utilisateur standard, admin, acces par id, mutations interdites, cochage d'item et creation owner.
- Les vues listes affichent le proprietaire pour les administrateurs.

### Critères d'acceptation

- Un utilisateur A ne voit jamais les listes de l'utilisateur B.
- Un utilisateur A ne peut pas deviner un `id` de liste et accéder à celle de B.
- Un admin peut lister et consulter toutes les listes.
- Les tests couvrent ces cas.

## Sprint 3 - Unicité plats et ingrédients

Objectif: éviter les doublons métier.

Avancement: `8/9 tâches - 89%`

### Tâches

- [x] Définir la règle d'unicité pour les ingrédients: `name` trim, casse ignorée, espaces multiples normalisés.
- [x] Définir la règle d'unicité pour les plats: `name` trim, casse ignorée, espaces multiples normalisés.
- [ ] Ajouter des index uniques MongoDB.
- [x] Normaliser côté back les noms: trim, casse, espaces multiples.
- [x] Empêcher la création d'un ingrédient existant.
- [x] Empêcher la création d'un plat existant.
- [x] Retourner `409 Conflict` en cas de doublon.
- [x] Adapter le front pour afficher un message clair.
- [x] Permettre la modification du nom d'un plat.

### Notes de vérification

- L'API bloque les nouveaux doublons sans migration destructive.
- Les nouveaux documents reçoivent un champ `nameKey` préparant les futurs index.
- Le front propose une recherche locale dans les écrans ingrédients et plats pour retrouver un élément avant création.
- Le front bloque aussi la création si le nom saisi correspond déjà à un ingrédient ou plat existant après normalisation simple.
- L'édition d'un plat s'ouvre maintenant directement sous le plat sélectionné, au lieu de descendre en bas du tableau.
- Les index uniques restent volontairement en attente: ils peuvent échouer si la base MongoDB contient déjà des doublons.
- Avant d'activer ces index, auditer les collections `ingredients` et `plats` sur la clé normalisée.

### Critères d'acceptation

- Impossible de créer deux ingrédients équivalents comme `Tomate`, ` tomate ` ou `TOMATE`.
- Impossible de créer deux plats équivalents.
- Le nom d'un plat peut être modifié.
- Les erreurs de doublon sont lisibles côté UI.

## Sprint 4 - Normalisation des unités et agrégation

Objectif: additionner correctement les ingrédients communs même si les plats utilisent des unités compatibles différentes.

Avancement: `6/6 tâches - 100%`

### Tâches

- [x] Définir un modèle d'unités canonique.
- [x] Ajouter une table ou un service de conversion pour les unités compatibles.
- [x] Convertir les quantités vers une unité canonique avant agrégation.
- [x] Gérer les cas non convertibles avec une règle explicite.
- [x] Aligner front et back sur la même liste d'unités.
- [x] Ajouter des tests sur les conversions et l'agrégation de listes.

### Notes de vérification

- Le back centralise désormais les unités autorisées dans un catalogue commun: `g`, `kg`, `ml`, `cl`, `l`, `paquet`, `unité`.
- L'agrégation convertit les masses vers `g` et les volumes vers `ml` avant addition.
- Quand un ingrédient apparaît avec des familles d'unités incompatibles, le détail de liste affiche plusieurs quantités explicites au lieu de masquer l'information.
- Les listes conservent maintenant les ingrédients manuels séparément des plats sélectionnés, ce qui évite les doubles comptages lors de l'édition.
- Vérifications passées: `dotnet run --project Back/ListeDeCourses.Api.Tests/Api.Tests.csproj --no-restore`, `npm run typecheck`, `npm run build`.

### Critères d'acceptation

- `500 g` + `1 kg` du même ingrédient devient `1.5 kg` ou `1500 g` selon la règle choisie.
- Les quantités ne disparaissent plus quand les unités sont compatibles.
- Les unités non compatibles restent visibles avec une indication claire.

## Sprint 5 - Lien recette sur les plats

Objectif: associer un lien externe à un plat.

Avancement: `5/5 tâches - 100%`

### Tâches

- [x] Ajouter un champ optionnel `recipeUrl` ou `sourceUrl` sur les plats.
- [x] Valider que le lien est une URL HTTP/HTTPS.
- [x] Afficher le lien dans la liste et le détail du plat.
- [x] Ouvrir le lien dans un nouvel onglet avec `rel="noopener noreferrer"`.
- [x] Préparer ce champ pour les futures fonctionnalités de récupération d'ingrédients.

### Notes de vérification

- Le champ implémenté est `sourceUrl`, disponible sur les DTOs, le modèle MongoDB et le contrat TypeScript.
- Les validateurs acceptent uniquement les URLs HTTP/HTTPS sans identifiants intégrés.
- La vue Plats permet d'ajouter, modifier, supprimer et ouvrir le lien source avec `noopener noreferrer`.
- Tests back ajoutés pour le mapping du champ et le rejet des URLs non sûres.

### Critères d'acceptation

- Un plat peut être créé et modifié avec ou sans lien.
- Les liens YouTube, Marmiton, TikTok et sites classiques sont acceptés si URL valides.
- Les URLs invalides sont rejetées côté back et expliquées côté front.

## Sprint 6 - Qualité front et mobile

Objectif: rendre l'interface plus fiable et confortable sur desktop et mobile.

Avancement: `0/10 tâches - 0%`

### Tâches

- [ ] Tester chaque écran sur mobile: accueil, login/register, ingrédients, plats, listes, détail liste, utilisateurs admin.
- [ ] Corriger les débordements, boutons trop petits, modales difficiles à utiliser.
- [ ] Corriger les tableaux/listes responsive: dernière ligne visible sur mobile, pas de barre horizontale inutile sur desktop, largeur adaptée à la fenêtre.
- [ ] Afficher dans le détail d'une liste les plats sélectionnés pour générer cette liste.
- [ ] Ajouter un bouton de création sur les pages ingrédients, plats et listes pour ouvrir une modale dédiée.
- [ ] Ajouter un bouton Annuler dans les modales de création d'ingrédient, plat et liste pour fermer la création sans enregistrer.
- [ ] Supprimer le blocage du zoom mobile sauf justification forte.
- [ ] Améliorer les états loading, empty, error.
- [ ] Ajouter confirmations cohérentes sur suppressions sensibles.
- [ ] Réduire les SVG dupliqués ou introduire une petite couche d'icônes réutilisables.

### Critères d'acceptation

- Toutes les fonctions principales sont utilisables sur écran mobile.
- Aucun texte ou bouton important ne déborde.
- Les dernières lignes des tableaux/listes restent accessibles sur mobile.
- Le détail d'une liste affiche aussi les plats sélectionnés, pas seulement les ingrédients agrégés.
- Les vues desktop s'adaptent à la largeur disponible sans scroll horizontal inutile.
- Les créations d'ingrédient, plat et liste passent par une modale ouvrable et annulable.
- Les erreurs API sont visibles et compréhensibles.
- Le zoom navigateur n'est pas bloqué sans raison d'accessibilité documentée.

## Sprint 7 - Dette back et performance

Objectif: solidifier les fondations sans changer inutilement le produit.

Avancement: `1/6 tâches - 17%`

### Tâches

- [ ] Remplacer les `GetAllAsync()` puis filtrages mémoire par des queries Mongo ciblées.
- [ ] Introduire pagination ou recherche pour les collections qui peuvent grossir.
- [x] Réduire les N+1 queries lors de l'agrégation des listes.
- [ ] Clarifier les responsabilités repositories/services.
- [ ] Unifier les réponses d'erreur API.
- [ ] Ajouter tests unitaires et tests d'intégration sur les règles métier.

### Notes de vérification

- L'agrégation des listes utilise désormais des lectures groupées `GetByIdsAsync` pour les plats et ingrédients.
- Un test back vérifie que la matérialisation d'une liste n'appelle plus les lookups unitaires lors de l'agrégation.
- Les stores front évitent les chargements redondants avec `ensureLoaded()` et sont vidés lors d'un changement de session.

### Critères d'acceptation

- Les endpoints principaux ne chargent pas toute la base sans besoin.
- Les règles critiques ont des tests.
- Les erreurs API sont cohérentes.

## Sprint 8 - Documentation et specs durables

Objectif: rendre le projet facile à reprendre.

Avancement: `1/5 tâches - 20%`

### Tâches

- [x] Mettre à jour le README avec stack réelle, commandes, variables d'environnement.
- [ ] Documenter les routes API.
- [ ] Documenter les modèles MongoDB actuels.
- [ ] Mettre à jour les specs après chaque décision métier.
- [ ] Tenir un backlog clair des tâches restantes.

### Notes de vérification

- Le README racine décrit maintenant la stack réelle, les URLs de déploiement, les commandes locales, les variables d'environnement et le comportement `manualItems`/`quantities`.
- Le README front remplace le template Vite par les commandes et le contrat API utilisés par l'application.

### Critères d'acceptation

- Un nouveau contributeur peut lancer le projet localement.
- Les choix métier importants sont documentés.
- Les prochaines tâches sont identifiables sans refaire toute la review.
