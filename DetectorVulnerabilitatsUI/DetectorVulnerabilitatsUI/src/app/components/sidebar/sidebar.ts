import { Component, model } from '@angular/core';

@Component({
  selector: 'sidebar',
  standalone: false,
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
// CLAU: 'model' crea una entrada que també es pot escriure cap a fora (two-way binding)
  activeTab = model.required<'dashboard' | 'scan' | 'results' | 'settings'>();

  selectTab(tab: 'dashboard' | 'scan' | 'results' | 'settings') {
    this.activeTab.set(tab); // Això actualitza automàticament el pare
  }

  getTabClass(tabName: string) {
    return this.activeTab() === tabName
      ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 shadow-lg shadow-emerald-500/5'
      : 'text-slate-400 hover:bg-slate-800 hover:text-white';
  }
}
