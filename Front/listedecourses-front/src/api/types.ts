export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface User {
  id: string
  email: string
  pseudo: string
  isSuperUser: boolean
}

export interface Ingredient {
  id: string
  name: string
  aisle: string | null
}

export interface DishIngredientRef {
  ingredientId: string
  quantity: number | null
  unit: string | null
}

export interface Dish {
  id: string
  name: string
  ingredients: DishIngredientRef[]
}

export interface ShoppingListItem {
  ingredientId: string
  ingredientName: string
  quantity: number | null
  unit: string | null
  aisle: string | null
  checked?: boolean | null
}

export interface ShoppingList {
  id: string
  name: string
  date: string
  items: ShoppingListItem[]
  dishIds: string[]
  ownerId: string
}

export interface ListCreateRequest {
  name: string
  date?: string
  items?: ShoppingListItem[]
  dishIds?: string[]
}

export interface ListUpdateRequest {
  name?: string
  date?: string
  items?: ShoppingListItem[]
  dishIds?: string[]
}

export interface UserCreateRequest {
  email: string
  pseudo: string
  password: string
  isSuperUser: boolean
}
