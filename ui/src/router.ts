import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import GroupView from './views/GroupView.vue'
import ManageView from './views/ManageView.vue'
import InsightsView from './views/InsightsView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/groups/:groupId', component: GroupView, props: true },
    { path: '/groups/:groupId/manage', component: ManageView, props: true },
    { path: '/groups/:groupId/insights', component: InsightsView, props: true },
  ],
})
