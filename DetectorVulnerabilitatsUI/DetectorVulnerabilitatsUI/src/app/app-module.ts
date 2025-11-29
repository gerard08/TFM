import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Sidebar } from './components/sidebar/sidebar';
import { Scan } from './components/scan/scan';
import { Results } from './components/results/results';

@NgModule({
  declarations: [
    App,
    Sidebar,
    Scan,
    Results
  ],
  imports: [
    BrowserModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners()
  ],
  bootstrap: [App]
})
export class AppModule { }
