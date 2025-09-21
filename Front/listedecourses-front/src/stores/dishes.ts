import { createCrudStore } from './_crudFactory'
import { endpoints } from '@/api/endpoints'
import type { Dish } from '@/api/types'

export const useDishesStore = createCrudStore<Dish>('dishes', endpoints.dishes)
