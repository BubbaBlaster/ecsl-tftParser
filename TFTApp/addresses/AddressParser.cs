using System;
using System.Net;
using System.Xml;

namespace addresses
{
    public class AddressParser
    {
        public string Address { get; set; }
        public string StreetNumber { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Zipcode { get; set; }
        public string Apartment { get; set; }


        private XmlDocument _doc;

        public void ParseUSPS()
        {
            // Your Username is 259EXCHA5814
            // Your Password is 980PE49LY328
            // http://production.shippingapis.com/ShippingAPI.dll

            try
            {
                using (var client = new WebClient())
                {
                    string query = "http://production.shippingapis.com/ShippingAPI.dll?API=Verify&" +
                        "XML=<AddressValidateRequest USERID = \"259EXCHA5814\">" +
                               "<ReturnCarrierRoute>true</ReturnCarrierRoute>" +
                               "<Address ID=\"0\">" +
                                 "<FirmName/>" +
                                 "<Address1/>" +
                                 "<Address2>" + Street + "</Address2>" +
                                 "<City>" + City + "</City>" +
                                 "<State>TX</State>" +
                                 "<Zip5>" + Zipcode + "</Zip5>" +
                                 "<Zip4/>" +
                               "</Address>" +
                             "</AddressValidateRequest>";
                    var contents = client.DownloadString(query);
                    _doc = new XmlDocument();
                    _doc.InnerXml = contents;
                    //<? xml version = "1.0" encoding = "UTF-8" ?>
                    //    < AddressValidateResponse >
                    //    < Address ID = "0" >
                    //    < Address2 > 15523 BRIAR SPRING CT </ Address2 >
                    //    < City > MISSOURI CITY </ City >
                    //    < State > TX </ State >
                    //    < Zip5 > 77489 </ Zip5 >
                    //    < Zip4 > 2806 </ Zip4 >

                    XmlNode n = _doc.SelectSingleNode("AddressValidateResponse/Address/Address2");
                    XmlNodeList nodes = _doc.SelectNodes("AddressValidateResponse/Address/Address2");
                    Street = (n == null) ? "ERROR" : n.InnerText;
                    n = _doc.SelectSingleNode("AddressValidateResponse/Address/City");
                    City = (n == null) ? "ERROR" : n.InnerText + ", TX";
                    n = _doc.SelectSingleNode("AddressValidateResponse/Address/Zip5");
                    Zipcode = (n == null) ? "ERROR" : n.InnerText;
                }
            }
            catch(Exception)
            {
                Street = "ERROR";
                City = "ERROR";
                Zipcode = "ERROR";
            }
        }

        public void ParseGoogleAPI(string streetNumber)
        {
            try
            {
                using (var client = new WebClient())
                {
                    string add = Address.Replace(' ', '+');
                    var contents = client.DownloadString("https://maps.googleapis.com/maps/api/geocode/xml?address=" + add);
                    _doc = new XmlDocument();
                    _doc.InnerXml = contents;

                    City = GetLongName("locality") == "ERROR" ? "ERROR" : GetLongName("locality") + ", " + GetLongName("administrative_area_level_1");
                    if (City.Contains("Kendle"))
                    {
                        County = "Fort Bend County";
                        Zipcode = "77451";
                    }
                    else
                    {
                        County = GetLongName("administrative_area_level_2");
                        Zipcode = GetLongName("postal_code");
                        if (County == "ERROR" && (Zipcode == "77083" ||
                            Zipcode == "77498"))
                            County = "Fort Bend County";
                    }
                    StreetNumber = streetNumber;
                    Street = GetLongName("route");
                }
            }
            catch
            {
                City = 
                County = 
                Street =
                StreetNumber =
                Zipcode = "ERROR";
            }
        }

        string GetLongName(string type)
        {
            XmlNodeList nodeList = _doc.SelectNodes("GeocodeResponse/result/address_component[type='" + type + "']/long_name");
            return nodeList.Count == 1 ? nodeList[0].InnerText : "ERROR";
        }
    }
}
