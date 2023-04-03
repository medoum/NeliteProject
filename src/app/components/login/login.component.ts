import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import  ValidateForm from 'src/app/helpers/validateform';
import { AuthService } from 'src/app/services/auth.service';
import { NgToastService } from 'ng-angular-popup';
import { UserStoreService } from 'src/app/user-store.service';
import { ResetPasswordService } from 'src/app/reset-password.service';
@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  public loginForm!: FormGroup;
  type: string = "password";
  isText: boolean = false;
  eyeIcon: string = "fa-eye-slash";
  public resetPasswordEmail! :string;
  public isValidEmail! :boolean;

  // Injecting the auth service and the router to do the Api calling
  constructor(
     private fb: FormBuilder,
     private auth: AuthService, 
     private router: Router,
     private toast: NgToastService,
     private userStore : UserStoreService,
     private resetService: ResetPasswordService
   ){}  

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', Validators.required],
    })
  }
  hideShowPass(){
    this.isText = !this.isText;
    this.isText ? this.eyeIcon = "fa-eye" : this.eyeIcon = "fa-eye-slash";
    this.isText ? this.type = "text" : this.type = "password";
  }
  
  onSubmit(){
    // if the form is valid
    if(this.loginForm.valid){
      console.log(this.loginForm.value);
      // Send the obj to database
      this.auth.signIn(this.loginForm.value).subscribe({
        next: (res) => {
          console.log(res.message);
          // Reset the form
          this.loginForm.reset();
          this.auth.storeToken(res.accessToken);
          this.auth.storeRefreshToken(res.refreshToken);
          const tokenPayload = this.auth.decodedToken();
          this.userStore.setFullNameForStore(tokenPayload.name);
          this.userStore.setRoleForStore(tokenPayload.role);
          this.toast.success({detail:"SUCCESS", summary:res.message, duration: 5000});
          // sending to the dashboard page
          this.router.navigate(['dashboard'])
        },
        error: (err) => {
             this.toast.error({detail:"ERROR", summary:"Something went wrong!", duration: 5000});
            console.log(err);
        },
      });
    }else{
      ValidateForm.validateAllFormFields(this.loginForm)
      // logic for thworing error
    }
 
  }
    checkValidEmail(event: string){
      const value = event;
      const pattern = /^[\w-\.]+@([\w-]+\.)+[\w-]{2,3}$/;
      this.isValidEmail = pattern.test(value);
      return this.isValidEmail;
    }

    confirmToSend(){
      if(this.checkValidEmail(this.resetPasswordEmail)){
        console.log(this.resetPasswordEmail);
        this.resetPasswordEmail = "";
        const buttonRef = document.getElementById("closeBtn");
        buttonRef?.click();
        //API call to be done

        this.resetService.sendResetPasswordLink(this.resetPasswordEmail)
        .subscribe({
          next:(res) =>{
           this.toast.success({
              detail: 'Success',
              summary: 'Reset Succes!',
              duration: 3000,
           });
            const buttonRef = document.getElementById("closeBtn");
            buttonRef?.click();
          },
          error:(err)=>{
            this.toast.error({
              detail:"ERROR", 
              summary:"Something went wrong!", 
              duration: 3000,
            });
          }
        })
      }
    }
  }

 
  
  
