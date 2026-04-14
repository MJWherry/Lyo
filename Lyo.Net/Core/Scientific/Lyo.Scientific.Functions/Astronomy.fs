namespace Lyo.Scientific.Functions

open System
open Lyo.Mathematics.Quantities
open Lyo.Scientific
open Lyo.Scientific.Astronomy

[<AbstractClass; Sealed>]
type AstronomyFunctions private () =
    static member AllPlanetaryBodies() = PlanetaryBodies.All |> Seq.toArray

    static member GetPlanetaryBodyByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        PlanetaryBodies.All
        |> Seq.tryFind (fun body -> String.Equals(body.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No planetary body exists for the provided name.", nameof name)))

    static member SurfaceGravity(body: PlanetaryBody) =
        match box body with
        | null -> raise (ArgumentNullException(nameof body))
        | _ -> ()

        let radius = body.MeanRadius.Meters

        Acceleration.FromMetersPerSecondSquared(
            ScientificConstants.GravitationalConstant * body.Mass.Kilograms
            / (radius * radius)
        )

    static member EscapeVelocity(body: PlanetaryBody) =
        match box body with
        | null -> raise (ArgumentNullException(nameof body))
        | _ -> ()

        let radius = body.MeanRadius.Meters
        Velocity.FromMetersPerSecond(Math.Sqrt(2.0 * ScientificConstants.GravitationalConstant * body.Mass.Kilograms / radius))

    static member OrbitalCircumference(body: PlanetaryBody) =
        match box body with
        | null -> raise (ArgumentNullException(nameof body))
        | _ -> ()

        Length.FromMeters(2.0 * ScientificConstants.Pi * body.SemiMajorAxis.Meters)

    static member OrbitalVelocity(centralMass: Mass, orbitalRadius: Length) =
        let radius = orbitalRadius.Meters
        Velocity.FromMetersPerSecond(Math.Sqrt(ScientificConstants.GravitationalConstant * centralMass.Kilograms / radius))

    static member OrbitalPeriod(centralMass: Mass, orbitalRadius: Length) =
        let radius = orbitalRadius.Meters
        TimeInterval.FromSeconds(
            2.0
            * ScientificConstants.Pi
            * Math.Sqrt(
                Math.Pow(radius, 3.0)
                / (ScientificConstants.GravitationalConstant * centralMass.Kilograms)
            )
        )

    static member SurfaceFlux(luminosity: Power, distance: Length) =
        let radius = distance.Meters
        luminosity.Watts / (4.0 * ScientificConstants.Pi * radius * radius)

    static member EquilibriumTemperature(luminosity: Power, orbitalDistance: Length, albedo: double) =
        let reflectivity = ScientificGuard.finite (nameof albedo) albedo

        if reflectivity < 0.0 || reflectivity > 1.0 then
            raise (ArgumentOutOfRangeException(nameof albedo))

        let flux = AstronomyFunctions.SurfaceFlux(luminosity, orbitalDistance)
        Temperature.FromKelvin(Math.Pow((flux * (1.0 - reflectivity)) / (4.0 * 5.670374419e-8), 0.25))

    static member DistanceInAstronomicalUnits(distance: Length) =
        distance.Meters / AstronomyReferenceValues.AstronomicalUnit.Meters

    static member DistanceFromAstronomicalUnits(astronomicalUnits: double) =
        Length.FromMeters(
            ScientificGuard.nonNegativeFinite (nameof astronomicalUnits) astronomicalUnits
            * AstronomyReferenceValues.AstronomicalUnit.Meters
        )

    static member ApparentFluxFromLuminosity(luminosity: Power, distance: Length) =
        AstronomyFunctions.SurfaceFlux(luminosity, distance)

    static member GetStarByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        StellarBodies.All
        |> Seq.tryFind (fun star -> String.Equals(star.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No star exists for the provided name.", nameof name)))

    static member GetMoonByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        NaturalSatellites.All
        |> Seq.tryFind (fun moon -> String.Equals(moon.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No moon exists for the provided name.", nameof name)))

    static member GetExoplanetByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        Exoplanets.All
        |> Seq.tryFind (fun planet -> String.Equals(planet.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No exoplanet exists for the provided name.", nameof name)))

    static member GetAsteroidByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        SmallBodies.Asteroids
        |> Seq.tryFind (fun asteroid -> String.Equals(asteroid.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No asteroid exists for the provided name.", nameof name)))

    static member GetCometByName(name: string) =
        let normalized = ScientificGuard.nonEmptyString (nameof name) name

        SmallBodies.Comets
        |> Seq.tryFind (fun comet -> String.Equals(comet.Name, normalized, StringComparison.OrdinalIgnoreCase))
        |> Option.defaultWith (fun () -> raise (ArgumentException("No comet exists for the provided name.", nameof name)))

    static member PeriapsisDistance(elements: OrbitalElements) =
        Length.FromMeters(elements.SemiMajorAxis.Meters * (1.0 - elements.Eccentricity))

    static member ApoapsisDistance(elements: OrbitalElements) =
        Length.FromMeters(elements.SemiMajorAxis.Meters * (1.0 + elements.Eccentricity))

    static member MeanMotion(centralMass: Mass, elements: OrbitalElements) =
        let period = AstronomyFunctions.OrbitalPeriod(centralMass, elements.SemiMajorAxis)
        2.0 * ScientificConstants.Pi / period.Seconds

    static member SemiMajorAxisFromPeriod(centralMass: Mass, orbitalPeriod: TimeInterval) =
        let term =
            ScientificConstants.GravitationalConstant
            * centralMass.Kilograms
            * Math.Pow(orbitalPeriod.Seconds / (2.0 * ScientificConstants.Pi), 2.0)

        Length.FromMeters(Math.Pow(term, 1.0 / 3.0))

    static member LuminosityRatioFromMagnitudeDifference(magnitudeDifference: double) =
        Math.Pow(10.0, -magnitudeDifference / 2.5)

    static member MagnitudeDifferenceFromLuminosityRatio(luminosityRatio: double) =
        let ratio = ScientificGuard.positiveFinite (nameof luminosityRatio) luminosityRatio
        -2.5 * Math.Log10(ratio)

    static member AbsoluteMagnitudeFromLuminosity(luminosity: Power, referenceLuminosity: Power) =
        AstronomyFunctions.MagnitudeDifferenceFromLuminosityRatio(luminosity.Watts / referenceLuminosity.Watts)

    static member ApparentMagnitudeFromFlux(flux: double, referenceFlux: double) =
        let current = ScientificGuard.positiveFinite (nameof flux) flux
        let reference = ScientificGuard.positiveFinite (nameof referenceFlux) referenceFlux
        -2.5 * Math.Log10(current / reference)