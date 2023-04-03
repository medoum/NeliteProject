import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import ValidateForm from 'src/app/helpers/validateform';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent {
  type: string = "password";
  isText: boolean = false;
  eyeIcon: string = "fa-eye-slash";
  signUpForm!: FormGroup;

  // Injecting the auth service and the router to do the Api calling
  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router ){}

  // Validation

  ngOnInit(): void {
    this.signUpForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      userName: ['', Validators.required],
      email:['', Validators.required],
      password: ['', Validators.required],
    })
  }
  
  hideShowPass(){
    this.isText = !this.isText;
    this.isText ? this.eyeIcon = "fa-eye" : this.eyeIcon = "fa-eye-slash";
    this.isText ? this.type = "text" : this.type = "password";
  }

  onSignUp(){
    if(this.signUpForm.valid){
      // perform logic for sign up

      // Calling the method after injectin the service
      this.auth.signUp(this.signUpForm.value)
      
      // if it success
      .subscribe({
        next:(res=>{
          // the alerte message
          alert(res.message);
          // reset the form
          this.signUpForm.reset();
          // sending to the login page
          this.router.navigate(['login']);
        }),
        error:(err=> {
            alert(err?.error.message)
        }),
      })
    }else{
      ValidateForm.validateAllFormFields(this.signUpForm)
      // logic for thworing error
    }
  }
}