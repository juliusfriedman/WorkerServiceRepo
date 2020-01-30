using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerServiceRepo
{
    class NtsHelper
    {

        internal static NetTopologySuite.Geometries.GeometryFactory GeometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4269); //Todo, label

        public void Test() 
        {

            MultiLineString mls = GeometryFactory.CreateMultiLineString();

            LineString ls = GeometryFactory.CreateLineString();

            var numGeom = ls.NumGeometries;

            Polygon p = null;
            
            Geometry union = mls.Union(ls.Normalized());

        }

        /// <summary>
        /// Adheres to the interface defined at <see href="https://developers.google.com/maps/documentation/javascript/reference/coordinates#LatLng">Google Docs</see> but adds altitude
        /// </summary>
        public class LatLngAlt
        {
            internal const double DoubleZero = 0.0;
            internal const long LongZero = 0L;
            public double? lat { get; set; }
            public double? lng { get; set; }
            public double? altitude { get; set; }
            public LatLngAlt() { }
            public static bool IsValid(LatLngAlt @this)
            {
                return null != @this && @this.lat.HasValue && @this.lng.HasValue &&
                    double.IsFinite(@this.lat.Value) && double.IsFinite(@this.lng.Value) && @this.lat.Value != DoubleZero && @this.lng.Value != DoubleZero;
            }
            public static NetTopologySuite.Geometries.Point ToPoint(LatLngAlt @this, bool withAltitude = false)
            {
                return GeometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(@this.lng.GetValueOrDefault(), @this.lat.GetValueOrDefault()));
                //withAltitude && @this.altitude.HasValue && @this.altitude.Value != DoubleZero ? @this.altitude.Value : double.NaN));
            }
        }

        /// <summary>
        /// Adheres to the interface defined at <see href="https://developers.google.com/maps/documentation/javascript/reference/coordinates#LatLngBoundsLiteral">Google Docs</see>, adds altitude and also encompasses a southwest, northeast version.
        /// </summary>
        public class LatLngBounds
        {
            public double? north { get; set; }
            public double? south { get; set; }
            public double? east { get; set; }
            public double? west { get; set; }
            //Either or are supported
            public LatLngAlt southwest { get; set; }
            public LatLngAlt northeast { get; set; }
            //With optional altitude.
            public double? altitude { get; set; }
            public LatLngBounds() { }
            public static bool IsValid(LatLngBounds @this)
            {
                return null != @this && (LatLngAlt.IsValid(@this.southwest) && LatLngAlt.IsValid(@this.northeast)) ||
                    (@this.north.HasValue && @this.south.HasValue && @this.east.HasValue && @this.west.HasValue &&
                    double.IsFinite(@this.north.Value) && double.IsFinite(@this.south.Value) && double.IsFinite(@this.east.Value) && double.IsFinite(@this.west.Value) &&
                    @this.north.Value != LatLngAlt.DoubleZero && @this.south.Value != LatLngAlt.DoubleZero && @this.east.Value != LatLngAlt.DoubleZero && @this.west.Value != LatLngAlt.DoubleZero);
            }
            public static NetTopologySuite.Geometries.Polygon ToPolygon(LatLngBounds @this)
            {
                //If north east was passed the south and west and the other are embedded within that so extrapolate them
                if (null != @this.northeast)
                {
                    @this.south = @this.southwest.lat;
                    @this.west = @this.southwest.lng;
                    @this.north = @this.northeast.lat;
                    @this.east = @this.northeast.lng;
                }

                return  GeometryFactory.CreatePolygon(new NetTopologySuite.Geometries.Coordinate[]
                           {
                            new NetTopologySuite.Geometries.Coordinate()
                            {
                                Y = @this.south.GetValueOrDefault(),
                                X = @this.west.GetValueOrDefault()
                            },
                             new NetTopologySuite.Geometries.Coordinate()
                            {
                                Y = @this.south.GetValueOrDefault(),
                                X = @this.east.GetValueOrDefault()
                            },
                             new NetTopologySuite.Geometries.Coordinate()
                            {
                                Y = @this.north.GetValueOrDefault(),
                                X = @this.east.GetValueOrDefault()
                            },
                            new NetTopologySuite.Geometries.Coordinate()
                            {
                                Y = @this.north.GetValueOrDefault(),
                                X = @this.west.GetValueOrDefault()
                            },
                            new NetTopologySuite.Geometries.Coordinate()
                            {
                                Y = @this.south.GetValueOrDefault(),
                                X = @this.west.GetValueOrDefault()
                            }
                           });
            }
        }

    }
}
