import { createCrudStore } from './_crudFactory'
import { endpoints } from '@/api/endpoints'
import type { Ingredient } from '@/api/types'

export const useIngredientsStore = createCrudStore<Ingredient>('ingredients', endpoints.ingredients)
