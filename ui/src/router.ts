import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import GroupLayout from './views/GroupLayout.vue'
import GroupView from './views/GroupView.vue'
import ManageView from './views/ManageView.vue'
import InsightsView from './views/InsightsView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    {
      path: '/groups/:groupId',
      component: GroupLayout,
      props: true,
      children: [
        { path: '', component: GroupView },
        { path: 'manage', component: ManageView },
        { path: 'insights', component: InsightsView },
      ],
    },
  ],
})
