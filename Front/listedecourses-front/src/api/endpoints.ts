export const endpoints = {
  auth: {
    login: '/auth/login',
    profile: '/auth/me',
  },
  users: '/utilisateurs',
  ingredients: '/ingredients',
  dishes: '/plats',
  lists: '/listes',
} as const
