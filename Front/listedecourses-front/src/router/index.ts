import { createRouter, createWebHistory, NavigationGuardNext, RouteLocationNormalized } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const HomeView = () => import('@/views/HomeView.vue')
const UsersView = () => import('@/views/users/UsersView.vue')
const IngredientsView = () => import('@/views/ingredients/IngredientsView.vue')
const DishesView = () => import('@/views/dishes/DishesView.vue')
const ShoppingListsView = () => import('@/views/lists/ShoppingListsView.vue')
const ListDetailView = () => import('@/views/lists/ListDetailView.vue')

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    {
      path: '/users',
      name: 'users',
      component: UsersView,
      meta: { requiresAuth: true, requiresSuper: true },
    },
    {
      path: '/ingredients',
      name: 'ingredients',
      component: IngredientsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/dishes',
      name: 'dishes',
      component: DishesView,
      meta: { requiresAuth: true },
    },
    {
      path: '/lists',
      name: 'lists',
      component: ShoppingListsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/lists/:id',
      name: 'list-detail',
      component: ListDetailView,
      meta: { requiresAuth: true },
      props: true,
    },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
  scrollBehavior() {
    return { top: 0 }
  },
})

let didBootstrap = false

router.beforeEach(async (to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) => {
  const auth = useAuthStore()

  if (!didBootstrap) {
    auth.initFromLocalStorage()
    didBootstrap = true
  }

  if (to.meta.requiresAuth) {
    if (!auth.isAuthenticated) {
      const redirect = to.fullPath !== '/' ? to.fullPath : undefined
      return next({ path: '/', query: redirect ? { redirect } : undefined })
    }

    if (!auth.profile) {
      try {
        await auth.loadProfile()
      } catch {
        const redirect = to.fullPath !== '/' ? to.fullPath : undefined
        return next({ path: '/', query: redirect ? { redirect } : undefined })
      }
    }

    if (to.meta.requiresSuper && auth.profile?.isSuperUser !== true) {
      const redirect = to.fullPath !== '/' ? to.fullPath : undefined
      return next({ path: '/', query: redirect ? { redirect } : undefined })
    }
  }

  return next()
})

export default router
