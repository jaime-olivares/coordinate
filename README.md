# coordinate
Classes to store, handle, and retrieve geodesic coordinates, in memory, database, and XML, according to the ISO 6709 standard

Most GPS/geodesic libraries do some tasks around a latitude/longitude pair, but they don't handle the coordinate storage with care. This C# library provides a solution to manage coordinates at both memory and persistent storage (XML or database), complying with the [ISO 6709 standard](http://en.wikipedia.org/wiki/ISO_6709) (Annex H - Text string representation), in concordance with the World Wide Web Consortium's LatitudeLongitudeAltitude workgroup.

## In-memory storage
A geodesic coordinate has two main components: latitude and longitude. Both are expressed in sexagesimal degrees and decimals, according to ISO standards, with the following constraints:

- Positive values for North and East hemispheres
- Negative values for South and West hemispheres
- Latitude values from -90.0 to +90.0
- Longitude values from -180 to +180.0

The coordinate components will be stored as "seconds of degree" in `float` variables. This way, all coordinate elements (degrees, minutes, and seconds) will remain on the integral portion of values, with the exception of decimal of seconds, avoiding loss of precision. The first approach to implement a coordinate class would be then:

    class Coordinate
    {
        float latitude;
        float longitude;
    }

A XML representation would be:

    <Coordinate>
        <Latitude>-18781.3</Latitude>
        <Longitude>-290269.5</Longitude>
    </Coordinate>
    
This is barely 90 - 100 bytes per coordinate, including spacing characters. A 100,000 node geographic file will require nearly 10 MB of file storage! Here is where ISO comes to the rescue. A compliant representation for the sample data would be, among others:

    -05.2169-080.6303/ expressed in pure degrees, or
    -051301.3-0803749.5/ expressed in degrees, minutes, and seconds

By overriding the default serializer for the `Coordinate` class to use the second version, the resulting XML will look like: 

    <Coordinate>-051301.3-0803749.5/</Coordinate>
    
This is a storage saving of more than 50%. Also notice that, besides the fact that the class stores data in-memory conveniently in "seconds of degree", the XML storage is expressed in degrees, minutes, and seconds, so it is human-readable, without conversion formulas.

Coordinate class implementation
The first part of the Coordinate class implementation has the following fields and properties declarations (abbreviated):

    public class Coordinate : ICloneable, IXmlSerializable, IFormattable
    {
    // Expressed in seconds of degree, positive values for north
    private float latitude;
    // Expressed in seconds of degree, positive values for east
    private float longitude;

    public Coordinate()
    {
        Latitude = Longitude = 0.0f;
    }
    public Coordinate(float lat, float lon)
    {
        Latitude = lat;
        Longitude = lon;
    }
     
    public float Latitude
    {
        set { latitude = value * 3600.0f; }
        get  { return latitude / 3600.0f; } // return degrees 
    }
    public float Longitude
    {
        set { longitude = value * 3600.0f; }
        get { return longitude / 3600.0f; } // return degrees
    }
    
Latitude and longitude fields have been hidden from external usage and serialization. There are a couple of properties instead, Latitude and Longitude (with lead uppercase character); they can be accessed by the user, and return values as degrees and decimals, hiding the underlying storage format (seconds of degrees).

There are more public methods, to either set and get values in a friendly way:

    // Multi-argument setters
    public void SetD(float latDeg, float lonDeg) {...}
    public void SetDM(float latDeg, float latMin, bool north, float lonDeg, float lonMin, bool east) {...}
    public void SetDMS(float latDeg, float latMin, float latSec, bool north, float lonDeg, float lonMin, float lonSec, bool east) {...}

    // Multi-argument getters
    public void GetD(out float latDeg, out float lonDeg) {...}
    public void GetDM(out float latDeg, out float latMin, out bool north, out float lonDeg, out float lonMin, out bool east) {...}
    public void GetDMS(out float latDeg, out float latMin, out float latSec, out bool north, out float lonDeg, out float lonMin, out float lonSec, out bool east) {...}

    // Distance in meters
    public float Distance(Coordinate other) {...}

    // Parsing method
    public void ParseIsoString(string isoStr) {...}

The first version of the setter, `SetD`, just needs two arguments expressed in degrees and decimal with proper sign for the hemisphere. It is equivalent to setting the Latitude and Longitude properties independently. The following two overrides will need minutes and hemisphere explicitly, and seconds optionally. In this case, the degrees arguments should not contain the hemisphere sign. The getter has a one-by one correspondence with the setters.

Also, there a method to calculate the distance against other coordinate. It is implemented using the classical Haversine formula (web references inside the code). The result is expressed in meters, the ISO unit for distances.

The following methods in the code implement some fundamental overrides:

    public override string ToString() {...}
    public override bool Equals(object obj) {...}
    public override int GetHashCode() {...}

The default implementation of `Coordinate.ToString()` will return a string with the coordinate expressed in degrees, minutes, and seconds. There will be more implementations of this method. Equals() compares both the latitude and longitude values for equality, and `GetHashCode()` returns a hash value based on the lat/lon values. The following code implements the IFormattable interface, allowing to display coordinates in different fashions:

    // Not really IFormattable member
    public string ToString(string format) {...}
    // ToString version with formatting
    public string ToString(string format, IFormatProvider formatProvider) {...}

As the `IFormattable.ToString()` method accepts a second argument that is unused in this implementation (formatProvider), an abbreviated override has been added with just an argument: the format string. Valid format strings are the following:

- "D": 05.2169ºS 080.6303ºW
- "DM": 05º13.02'S 080º37.82'W
- "DMS": 05º13'01.3"S 080º37'49.5"W (default)
- "ISO": -051301.3-0803749.5/

Any other formatting string will produce an exception. Calling any version with an empty or null formatting string will output the default version. An additional benefit of the IFormattable interface is the capability for embeding the formatting string inside a more complex formatting case; for example:

    string s = string.Format("Sample coordinate: {0:DM}\r\n.", someCoord);
    
Finally, there is the `IXmlSerializable` implementation. It overrides the default XML formatting, as explained earlier. Here is the abbreviated source code:

    System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {...}
    void IXmlSerializable.ReadXml(XmlReader reader) {...}
    void IXmlSerializable.WriteXml(XmlWriter writer) {...}

Those methods will be invoked by any serialization function, as in the test source code below. `WriteXml()` will produce XML code for storing the latitude and longitude in ISO format with the pattern: "±DDMMSS.S±DDDMMSS.S/". `ReadXml()`, countersense, will parse a coordinate stored with any valid ISO format, even those including the height or depth, not considered for this implementation. An incorrect format will produce an exception.

## Coordinate collections
Once using the `Coordinate` class, it will be noticed another source of space wasting. Consider the following XML example:

    <CoordinateList>
        <Coordinate>+010000.0+0010000.0/</Coordinate>
        <Coordinate>-022400.0+0013000.0/</Coordinate>
        <Coordinate>-034200.0+0021200.0/</Coordinate>
        <Coordinate>-020000.0-0003000.0/</Coordinate>
    </CoordinateList>

A real-world GIS application will need hundreds of `Coordinate` entries for each polygon. So, the ISO standard specifies a more compact storage foramt:

    <CoordinateList>+010000.0+0010000.0/-022400.0+0013000.0/ -034200.0+0021200.0/-020000.0-0003000.0/</CoordinateList>

Again, there is a storage saving of more than 50%. The `CoordinateList` class has been implemented by deriving a generic `List<Coordinate>` collection and overriding the serialization methods from the `IXmlSerializable` interface.
Here is the template:

    public class CoordinateList : List<Coordinate>, IXmlSerializable
    {
       public CoordinateList() {...}
 
       public override string ToString() {...}

       public void ParseIsoString(string isoStr) {...}

       XmlSchema IXmlSerializable.GetSchema() {...}
       void IXmlSerializable.ReadXml(XmlReader reader) {...}
       void IXmlSerializable.WriteXml(XmlWriter writer) {...}
    }
    
## Database storage/retrieving
A single coordinate or a coordinate collection can be stored in any text field type into a database, by using the proper `ToString()` method, as in the following example:

    // Single point example
    string query = string.Format("INSERT INTO PointMarks SET ID={0}, LOCATION='{1:ISO}'", someID, coord);

    // Coordinate list example
    string query = string.Format("INSERT INTO Boundaries SET ID={0}, POLYGON='{1}'", someID, coord_list);
    
Notice that when inserting a single coordinate, the "ISO" formatting shall be used.

To retrieve a single coordinate or coordinate collection, create a new object and invoke the `ParseIsoString()` method, passing the string retrieved from the database, like in the following example. A try/catch block also will be recommendable, to avoid an unexpected exception.

    // Single point example
    string iso = datareader["LOCATION"].ToString(); 
    Coordinate coord = new Coordinate();
    coord.ParseIsoString(iso);

    // Coordinate list example
    string iso = datareader["POLYGON"].ToString(); 
    CoordinateList coord_list = new CoordinateList();
    coord_list.ParseIsoString(iso);
    
## The sample code
The supplied sample code in the `Program.Main()` method will do some little tasks to demonstrate the functionalities of the `Coordinate` and `CoordinateList` classes. 

## Why use float?
I have received many questions about why I use the `float` data type, rather than the `Int32`, `double`, or `decimal` data types. Here are some reasons about the use of float against `Int32` (expressed in milliseconds of degree or other scales):

- There is no meaningful advantage in storing size with an `Int32`, since `float` is 32-bits too.
- When calculating coordinates' components (D/M/S), floating point conversions will be needed anyway.
- When painting, it has to be converted to floating point.
- Also, float conversion will be needed for storing in ISO 6709 format.
- If expressed in milliseconds, the code will be less legible due to continuous multiplication/division by 1000.

About `float` against `decimal`, which is four times larger than float, the main disadvantage, besides the obvious space consumption, is performance. Calculations with `decimal` are 20-30 times slower than with `float`. This is due to the fact that `decimal` is stored as base-10 by microprocessors, rather than base-2 like all other data types, forcing it to do several conversions to perform any math calculation.

About `float` against `double`, there are concerns about precision that deserves a bigger explanation. First, I will mention the advantages of using `float`:

- The `double` data type (64 bits) needs twice the space as float (32 bits).
- Half the space and half the time to load in memory.
- The precision is enough compared to GPS precision (5-10 meters).

A longitude with a maximum value of 180 degrees at the Equator that has 60 nautical miles per minute. When stored in a float variable, will have a minimum precision of about 2.4 meters. However, the longitude magnitude will vary according to the latitude, with the precision varying down to zero at the poles. The precision will increase when the latitude is greater.

The latitude doesn't vary in any zone of the earth, with a constant magnitude of 60 nautical miles per minute of degree, and a maximum value of 90 in the poles. The most pessimistic calculation for the latitude (near to the poles) will be around 1.2 meters.

So, in practical terms, the average precision value using `float` will be smaller than a meter. This is more precise than a commercial GPS output, and suitable for most GIS applications up to street level.

