import { Route } from "@angular/router";
import { LoginComponent } from "./login/login.component";
import { RegisterComponent } from "./register/register.component";
import { authGuard } from "../../core/guards/auth.guard";
import { ProfileComponent } from "./profile/profile.component";

export const accountRoutes: Route[] = [
    {path: 'login', component: LoginComponent},
    {path: 'register', component: RegisterComponent},
    {path: 'profile', component: ProfileComponent, canActivate: [authGuard]},
]
