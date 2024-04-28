namespace ApiHoteleria.Models
{
    public class Persons
    {
        public int person_id { get; set; }
        public int user_id { get; set; }
        public string name { get; set; }    
        public string identity_document { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
    }
}
