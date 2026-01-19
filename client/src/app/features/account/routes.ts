import { Route } from "@angular/router";
import { LoginComponent } from "./login/login.component";
import { RegisterComponent } from "./register/register.component";
import { authGuard } from "../../core/guards/auth.guard";
import { ProfileComponent } from "./profile/profile.component";
import { ForgotPasswordComponent } from "./forgot-password/forgot-password.component";
import { ResetPasswordComponent } from "./reset-password/reset-password.component";
import { ConfirmEmailComponent } from "./confirm-email/confirm-email.component";
import { ConfirmEmailChangeComponent } from "./confirm-email-change/confirm-email-change.component";

export const accountRoutes: Route[] = [
    {path: 'login', component: LoginComponent},
    {path: 'register', component: RegisterComponent},
    {path: 'forgot-password', component: ForgotPasswordComponent},
    {path: 'reset-password', component: ResetPasswordComponent},
    {path: 'confirm-email', component: ConfirmEmailComponent},
    {path: 'confirm-email-change', component: ConfirmEmailChangeComponent},
    {path: 'profile', component: ProfileComponent, canActivate: [authGuard]},
]
