﻿using System.ComponentModel.DataAnnotations;

using DataCore.Adapter;

#nullable disable warnings

namespace MqttAdapter {

    public class MyAdapterOptions : AdapterOptions {

        [Display(Description = "The MQTT server hostname")]
        [Required(ErrorMessage = "You must specify a hostname")]
        [MaxLength(500, ErrorMessage = "Hostname cannot be longer than 500 characters")]
        public string Hostname { get; set; }

        [Display(Description = "The port number for the MQTT server")]
        [Range(1, 65535, ErrorMessage = "Port must be in the range 1 - 65535")]
        public int Port { get; set; }

        [Display(Description = "A comma-delimited list of MQTT topics to subscribe to")]
        [Required(ErrorMessage = "You must specify a list of MQTT topics to subscribe to")]
        public string Topics { get; set; }

    }

}
