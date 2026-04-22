# ShopList
https://shop-list-two.vercel.app/
## 🛒 Ma liste de courses

Projet effectué à but personnel

---

## 📌 Étapes du projet

1. Réflexion autour des collections de ma base de données
2. Création de la base de données **NoSQL (MongoDB)**
3. Connexion et setup du projet **back-end**
4. Rédaction des **modèles (Models)**
5. Rédaction des **repositories (Repositories)**
6. Rédaction des **DTOs (Data Transfer Objects)**
7. Rédaction des **services (Services)**
8. Rédaction des **contrôleurs (Controllers)**
9. Test des **controllers** en local
10. Écriture de **tests unitaires**
11. Finalisation du **back-end**
12. Wireframe & maquettage du **site**
13. Développement du **front-end** étape par étape  
    ➝ **(Bande statique → Ajout contenu page par page)**

---

## 🗂️ Collections de la BDD (MongoDB)

### **📌 Utilisateurs**

```json
{
  "users": [
    {
      "_id": "ObjectId",
      "email": "string",
      "pseudo": "string",
      "passwordHash": "string",
      "isSuperUser": "boolean"
    }
  ]
}
```

### **📌 Plats**

```json
{
  "dishes": [
    {
      "_id": "ObjectId",
      "name": "string",
      "ingredients": [
        {
          "ingredientId": "ObjectId",
          "quantity": "string"
        }
      ]
    }
  ]
}
```

### **📌 Ingrédients**

```json
{
  "ingredients": [
    {
      "_id": "ObjectId",
      "name": "string",
      "category": "string",
      "defaultQuantity": "string"
    }
  ]
}
```

### **📌 Listes de courses**

```json
{
  "shopping_lists": [
    {
      "_id": "ObjectId",
      "userId": "ObjectId",
      "date": "Date",
      "name": "string",
      "items": [
        {
          "ingredientId": "ObjectId",
          "quantity": "string",
          "category": "string",
          "isChecked": "boolean"
        }
      ]
    }
  ]
}
```

### **📌 Rayons (Catégories) - Facultatif**

```json
{
  "categories": [
    {
      "_id": "ObjectId",
      "name": "string"
    }
  ]
}
```

---

## 📂 Arborescence du projet

```
/ListeDeCourses
│-- ListeDeCourses.Api/
│   │-- Controllers/
│   │   │-- UtilisateursController.cs
│   │   │-- PlatsController.cs
│   │   │-- IngredientsController.cs
│   │   │-- ListesController.cs
│   │
│   │-- Models/
│   │   │-- Utilisateur.cs
│   │   │-- Plat.cs
│   │   │-- Ingredient.cs
│   │   │-- Liste.cs
│   │
│   │-- DTOs/
│   │   │-- UtilisateurDTO.cs
│   │   │-- PlatDTO.cs
│   │   │-- IngredientDTO.cs
│   │   │-- ListeDTO.cs
│   │
│   │-- Services/
│   │   │-- UtilisateurService.cs
│   │   │-- PlatService.cs
│   │   │-- IngredientService.cs
│   │   │-- ListeService.cs
│   │
│   │-- Repositories/
│   │   │-- UtilisateurRepository.cs
│   │   │-- PlatRepository.cs
│   │   │-- IngredientRepository.cs
│   │   │-- ListeRepository.cs
│   │
│   │-- appsettings.json
│   │-- Program.cs
```

---

## 🚀 Objectif du projet

Créer un site permettant de **générer une liste de courses à partir de plats sélectionnés**. Chaque plat est associé à une liste d’ingrédients qui sont **ajoutés automatiquement** à la liste de courses. L’utilisateur peut également :

- Ajouter des **plats personnalisés** avec leurs ingrédients.
- Recevoir des **suggestions de plats** basées sur les ingrédients déjà présents.
- Organiser sa liste de courses par **rayons/catégories**.

💡 **Technologies utilisées** :

- **Back-end** : ASP.NET Core (.NET 8), MongoDB
- **Front-end** : (à définir)
- **Authentification** : (à implémenter, JWT ?)

---

## Commandes locales fiables

### Back-end

```sh
cd Back/ListeDeCourses/Api
dotnet restore ApiList.csproj
dotnet run
```

L'API utilise MongoDB en local par defaut via `mongodb://localhost:27017` et la base `ShopList`.
Elle ecoute en local sur `http://localhost:5145`.

### Front-end

```sh
cd Front/listedecourses-front
npm install
npm run dev
npm run typecheck
npm run lint
npm run build
```

En developpement, Vite proxy les appels `/api` vers `http://localhost:5145`.

### Audit dependances front

```sh
cd Front/listedecourses-front
npm audit --omit=dev
```

Le full audit peut encore signaler l'alerte dev-server `vite/esbuild`; sa correction demande une migration majeure de Vite et doit etre traitee dans une passe dediee.

