﻿using System.IO;
using System.Linq;
using Data;
using Microsoft.AspNetCore.Mvc;
using Models.Diagnosis;
using Serilog;
using System;
using System.Collections.Generic;

namespace WebAPI.Controllers
{
    [Route("[Controller]")]
    [ApiController]
    public class SurgeryController : ControllerBase
    {
        //Dependency Injection
        private readonly IRepository<Surgery> _repo;

        public SurgeryController(IRepository<Surgery> context)
        {
            _repo = context;
        }

        // GET: surgery/Get/All
        [HttpGet("Get/All")]//Gets list of all surgeries
        public IActionResult GetAll()
        {
            try
            {
                List<Surgery> s = _repo.GetAll().ToList();
                if (s.Count == 0)
                    throw new FileNotFoundException("No data found");
                return Ok(_repo.GetAll());
            }
            catch (Exception e)
            {

                Log.Error(e.Message);
                return BadRequest("Failed to update");//Logs all bad requests into separate file
            }
        }

        // GET: surgery/Get/{id}
        [HttpGet("Get/{id}")]//Gets surgery by Id
        public IActionResult GetById(int id)
        {
            try
            {
                if (_repo.GetById(id) == null)
                    throw new InvalidDataException("Invalid Id");
                return Ok(_repo.GetById(id));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return BadRequest("Failed to update");
            }
        }

        // DELETE surgery/delete/{Id}
        [HttpDelete("Delete/{id}")]//Deletes surgery by Id
        public IActionResult Delete([FromBody] Surgery p_surgery)
        {
            try
            {
                if (p_surgery == null)
                    throw new InvalidDataException("Delete failed!");
                _repo.Delete(p_surgery);
                _repo.Save();
                return Ok();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return BadRequest("Failed to update");

            }

        }

        // PUT surgery/Update/{Id}
        [HttpPut("Update/{id}")]//Updates surgery by Id
        public IActionResult Update(int id, [FromBody] Surgery p_surgery)
        {
            try
            {
                p_surgery.Id = id;
                _repo.Update(p_surgery);
                _repo.Save();
                return Ok();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return BadRequest("Failed to update");
            }
        }

        // POST surgery/Add
        [HttpPost("Add")]//Adds new surgery info
        public IActionResult Add([FromBody] Surgery p_surgery)
        {
            try
            {
                if (p_surgery == null)
                    throw new InvalidDataException("Invalid data!");
                _repo.Create(p_surgery);
                _repo.Save();
                return Created("Surgery/Add", p_surgery);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return BadRequest("Failed to update");
            }
        }
    }
}