import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgToastService } from 'ng-angular-popup';
import { ConfirmPasswordValidator } from 'src/app/helpers/confirm-password.validator';
import ValidateForm from 'src/app/helpers/validateform';
import { ResetPassword } from 'src/app/models/reset-password.model';
import { ResetPasswordService } from 'src/app/reset-password.service';

@Component({
  selector: 'app-reset',
  templateUrl: './reset.component.html',
  styleUrls: ['./reset.component.scss']
})
export class ResetComponent implements OnInit {

  resetPasswordForm!: FormGroup;
  emailToReset!: string;
  emailToken!: string;
  resetPasswordObj = new ResetPassword();

  constructor(private fb: FormBuilder, 
    private toast: NgToastService, private activatedRoute: ActivatedRoute, private resetService: ResetPasswordService, private router: Router){}

  ngOnInit(): void {
      this.resetPasswordForm = this.fb.group({
        password: [null, Validators.required],
        confirmPassword: [null, Validators.required]
      },{
       validator: ConfirmPasswordValidator("password", "confirmPassword")
  });
  this.activatedRoute.queryParams.subscribe(val=>{
    this.emailToReset = val['email'];
    let uriToken = val['code'];
    this.emailToken = uriToken.replace(/ /g,'+');
    console.log(this.emailToken);
    console.log(this.emailToReset);
  })
}
reset(){
  if(this.resetPasswordForm.valid){
    this.resetPasswordObj.email = this.emailToReset;
    this.resetPasswordObj.newPassword = this.resetPasswordForm.value.password;
    this.resetPasswordObj.confimPassword =this.resetPasswordForm.value.confimPassword;
    this.resetPasswordObj.emailToken = this.emailToken;
    this.resetService.resetPasword(this.resetPasswordObj)
    .subscribe({
      next:(res) =>{
        this.toast.success({
          detail: 'SUCCESS',
          summary: "Password reset Successfully",
          duration: 300,
        });
        this.router.navigate(['/'])
      },
      error:(err)=>{
        this.toast.error({
          detail: 'ERROR',
          summary: "Something went wrong!!",
          duration: 3000,
        })
      }
    })

  }else{
    ValidateForm.validateAllFormFields(this.resetPasswordForm);
  }
}
}