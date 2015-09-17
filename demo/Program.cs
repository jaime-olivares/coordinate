using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ISO_Classes
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Create a new coordinate with some values
            Coordinate c = new Coordinate();
            c.SetDMS(00, 10, 20.8f, true, 30, 40, 50.9f, false);

            // Create a new coordinate list with
            CoordinateList cl = new CoordinateList();
            cl.Add(new Coordinate( 1.0f, 1.0f));
            cl.Add(new Coordinate(-2.4f, 1.5f));
            cl.Add(new Coordinate(-3.7f, 2.2f));
            cl.Add(new Coordinate(-2.0f, -.5f));

            // Show formatting capabilities
            MessageBox.Show(
                string.Format("String Formatting:\r\nD: {0:D}\r\nDM: {0:DM}\r\nDMS: {0:DMS}\r\nISO: {0:ISO}", c), 
                "Formatting example");

            // Serialize coordinates to a string object
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.IO.StringWriter sw = new System.IO.StringWriter(sb);
            string part1, part2;

            // Single coordinate
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(Coordinate));
            xs.Serialize(sw, c);
            sw.Flush();
            part1 = sb.ToString();

            // Coordinate list
            sb.Length = 0;
            System.Xml.Serialization.XmlSerializer xsl = new System.Xml.Serialization.XmlSerializer(typeof(CoordinateList));
            xsl.Serialize(sw, cl);
            sw.Flush();
            part2 = sb.ToString();

            sw.Close();

            // Show serialized data
            MessageBox.Show(string.Format("Single coordinate:\r\n{0}\r\n\r\nCoordinate list:\r\n{1}", part1, part2), 
                "Serialization example");

            // Deserialize single coordinate from a string object
            System.IO.StringReader sr = new System.IO.StringReader(part1);
            Coordinate c1 = (Coordinate)xs.Deserialize(sr);
            sr.Close();

            // Deserialize coordinate list from a string object
            sr = new System.IO.StringReader(part2);
            CoordinateList cl1 = (CoordinateList)xsl.Deserialize(sr);
            sr.Close();

            // Show properties of deserialized data
            string message = string.Format(
                "Single coordinate:\r\n{0}\r\nLatitude: {1}°\r\nLongitude: {2}°\r\n\r\nCoordinate list:\r\n", 
                c1, c1.Latitude, c1.Longitude);
            foreach (Coordinate coord in cl1)
                message += coord.ToString() + "\r\n";
 
            MessageBox.Show(message, "Deserialization example");
        }
    }
}
