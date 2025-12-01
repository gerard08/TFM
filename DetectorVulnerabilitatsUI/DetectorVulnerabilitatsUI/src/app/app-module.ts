import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { Sidebar } from './components/sidebar/sidebar';
import { Scan } from './components/scan/scan';
import { Results } from './components/results/results';
import { provideHttpClient } from '@angular/common/http';
import { Dashboard } from './components/dashboard/dashboard';
import { Report } from './components/report/report';

@NgModule({
  declarations: [
    App,
    Sidebar,
    Scan,
    Results,
    Dashboard,
    Report
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,

    ToastrModule.forRoot({
      timeOut: 3000,
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
    })
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideAnimations()
  ],
  bootstrap: [App]
})
export class AppModule { }
