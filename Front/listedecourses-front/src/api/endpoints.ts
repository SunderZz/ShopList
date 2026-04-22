export const endpoints = {
  auth: {
    login: '/auth/login',
    register: '/auth/register',
    profile: '/auth/me',
  },
  users: '/utilisateurs',
  ingredients: '/ingredients',
  dishes: '/plats',
  lists: '/listes',
} as const
