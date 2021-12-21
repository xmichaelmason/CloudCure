import { Component, OnInit } from '@angular/core';
import { PatientService } from '../services/patient.service';
import { VitalsService } from '../services/vitals.service';

@Component({
  selector: 'app-vitalsview',
  templateUrl: './vitalsview.component.html',
  styleUrls: ['./vitalsview.component.css']
})
export class VitalsviewComponent implements OnInit {

  //vitalsExists: Boolean = true;
  VitalsId:        any = '';
  PatientId:       any = '';
  Systolic:        any = 120;
  Diastolic:       any = 80;
  OxygenSat:       any = 98;
  HeartRate:       any = 75;
  Temperature:     any = 98;
  RespiratoryRate: any = 15;
  Height:          any = 72;
  Weight:          any = 185;
  EncounterDate:   any = '';

  constructor(private vitalsApi: VitalsService, private patientApi: PatientService) {
    this.vitalsApi.GetByPatientId(2).subscribe(response => {
      console.log("accessed1")
      console.log(response)
    //this.vitalsExists = true;
    //Instantiating Vitals Variables
    this.Systolic =        response.systolic
    this.Diastolic =       response.diastolic
    this.OxygenSat =       response.oxygenSat
    this.HeartRate =       response.heartRate
    this.Temperature =     response.temperature
    this.RespiratoryRate = response.respiratoryRate
    this.Height =          response.height
    this.Weight =          response.weight
    this.EncounterDate =   response.encounterDate

    })

    // this.patientApi.GetById(2).subscribe(response => {
    //   console.log("accessed2")
    //   console.log(response)
    // })

    // this.vitalsApi.GetAll().subscribe(response => {
    //   console.log("accessed3")
    //   console.log(response)
    // })
   }

  ngOnInit(): void {
  }

}
