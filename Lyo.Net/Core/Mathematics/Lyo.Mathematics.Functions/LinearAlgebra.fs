namespace Lyo.Mathematics.Functions

open System
open Lyo.Mathematics.Models
open Lyo.Mathematics.Vectors
open Lyo.Mathematics.Matrices

[<AbstractClass; Sealed>]
type LinearAlgebraFunctions private () =
    static member Determinant(matrix: Matrix2x2) =
        (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21)

    static member Determinant(matrix: Matrix3x3) =
        (matrix.M11 * ((matrix.M22 * matrix.M33) - (matrix.M23 * matrix.M32)))
        - (matrix.M12 * ((matrix.M21 * matrix.M33) - (matrix.M23 * matrix.M31)))
        + (matrix.M13 * ((matrix.M21 * matrix.M32) - (matrix.M22 * matrix.M31)))

    static member Transpose(matrix: Matrix2x2) =
        Matrix2x2(matrix.M11, matrix.M21, matrix.M12, matrix.M22)

    static member Transpose(matrix: Matrix3x3) =
        Matrix3x3(matrix.M11, matrix.M21, matrix.M31, matrix.M12, matrix.M22, matrix.M32, matrix.M13, matrix.M23, matrix.M33)

    static member Multiply(left: Matrix2x2, right: Matrix2x2) =
        Matrix2x2(
            (left.M11 * right.M11) + (left.M12 * right.M21),
            (left.M11 * right.M12) + (left.M12 * right.M22),
            (left.M21 * right.M11) + (left.M22 * right.M21),
            (left.M21 * right.M12) + (left.M22 * right.M22)
        )

    static member Multiply(left: Matrix3x3, right: Matrix3x3) =
        Matrix3x3(
            (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31),
            (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32),
            (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33),
            (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31),
            (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32),
            (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33),
            (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31),
            (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32),
            (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33)
        )

    static member Multiply(matrix: Matrix2x2, vector: Vector2D) =
        Vector2D((matrix.M11 * vector.X) + (matrix.M12 * vector.Y), (matrix.M21 * vector.X) + (matrix.M22 * vector.Y))

    static member Multiply(matrix: Matrix3x3, vector: Vector3D) =
        Vector3D(
            (matrix.M11 * vector.X) + (matrix.M12 * vector.Y) + (matrix.M13 * vector.Z),
            (matrix.M21 * vector.X) + (matrix.M22 * vector.Y) + (matrix.M23 * vector.Z),
            (matrix.M31 * vector.X) + (matrix.M32 * vector.Y) + (matrix.M33 * vector.Z)
        )

    static member FrobeniusNorm(matrix: Matrix2x2) =
        Math.Sqrt(
            (matrix.M11 * matrix.M11)
            + (matrix.M12 * matrix.M12)
            + (matrix.M21 * matrix.M21)
            + (matrix.M22 * matrix.M22)
        )

    static member FrobeniusNorm(matrix: Matrix3x3) =
        Math.Sqrt(
            (matrix.M11 * matrix.M11)
            + (matrix.M12 * matrix.M12)
            + (matrix.M13 * matrix.M13)
            + (matrix.M21 * matrix.M21)
            + (matrix.M22 * matrix.M22)
            + (matrix.M23 * matrix.M23)
            + (matrix.M31 * matrix.M31)
            + (matrix.M32 * matrix.M32)
            + (matrix.M33 * matrix.M33)
        )

    static member Inverse(matrix: Matrix2x2) =
        let determinant = LinearAlgebraFunctions.Determinant(matrix)

        if determinant = 0.0 then
            raise (ArgumentException("Matrix is singular and cannot be inverted.", nameof matrix))

        let inverseDeterminant = 1.0 / determinant
        Matrix2x2(matrix.M22 * inverseDeterminant, -matrix.M12 * inverseDeterminant, -matrix.M21 * inverseDeterminant, matrix.M11 * inverseDeterminant)

    static member Inverse(matrix: Matrix3x3) =
        let determinant = LinearAlgebraFunctions.Determinant(matrix)

        if determinant = 0.0 then
            raise (ArgumentException("Matrix is singular and cannot be inverted.", nameof matrix))

        let c11 = (matrix.M22 * matrix.M33) - (matrix.M23 * matrix.M32)
        let c12 = -((matrix.M21 * matrix.M33) - (matrix.M23 * matrix.M31))
        let c13 = (matrix.M21 * matrix.M32) - (matrix.M22 * matrix.M31)
        let c21 = -((matrix.M12 * matrix.M33) - (matrix.M13 * matrix.M32))
        let c22 = (matrix.M11 * matrix.M33) - (matrix.M13 * matrix.M31)
        let c23 = -((matrix.M11 * matrix.M32) - (matrix.M12 * matrix.M31))
        let c31 = (matrix.M12 * matrix.M23) - (matrix.M13 * matrix.M22)
        let c32 = -((matrix.M11 * matrix.M23) - (matrix.M13 * matrix.M21))
        let c33 = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21)
        let inverseDeterminant = 1.0 / determinant

        Matrix3x3(
            c11 * inverseDeterminant,
            c21 * inverseDeterminant,
            c31 * inverseDeterminant,
            c12 * inverseDeterminant,
            c22 * inverseDeterminant,
            c32 * inverseDeterminant,
            c13 * inverseDeterminant,
            c23 * inverseDeterminant,
            c33 * inverseDeterminant
        )

    static member Solve3x3(matrix: Matrix3x3, vector: Vector3D) =
        let inverse = LinearAlgebraFunctions.Inverse(matrix)
        LinearAlgebraFunctions.Multiply(inverse, vector)

    static member Solve2x2(input: LinearSystem2x2Input) =
        let determinant = LinearAlgebraFunctions.Determinant(input.Matrix)

        if determinant = 0.0 then
            LinearSystem2x2Result(Vector2D(0.0, 0.0), false)
        else
            let x =
                ((input.Vector.X * input.Matrix.M22) - (input.Matrix.M12 * input.Vector.Y))
                / determinant

            let y =
                ((input.Matrix.M11 * input.Vector.Y) - (input.Vector.X * input.Matrix.M21))
                / determinant

            LinearSystem2x2Result(Vector2D(x, y), true)

    static member QrDecomposition(matrix: double[,]) =
        match box matrix with
        | null -> raise (ArgumentNullException(nameof matrix))
        | _ -> ()

        let rows = Array2D.length1 matrix
        let columns = Array2D.length2 matrix
        let q = Array2D.zeroCreate<float> rows columns
        let r = Array2D.zeroCreate<float> columns columns

        let getColumn index =
            Array.init rows (fun row -> matrix[row, index])

        let setColumn (target: float[,]) index (values: float array) =
            values |> Array.iteri (fun row value -> target[row, index] <- value)

        let dot (left: float array) (right: float array) = Array.map2 (*) left right |> Array.sum
        let norm (values: float array) = Math.Sqrt(dot values values)

        for j in 0 .. columns - 1 do
            let mutable v = getColumn j

            for i in 0 .. j - 1 do
                let qi = Array.init rows (fun row -> q[row, i])
                let projection = dot qi v
                r[i, j] <- projection
                v <- Array.map2 (fun current basis -> current - (projection * basis)) v qi

            let columnNorm = norm v

            if columnNorm = 0.0 then
                raise (ArgumentException("Matrix columns must be linearly independent for QR decomposition.", nameof matrix))

            r[j, j] <- columnNorm
            setColumn q j (v |> Array.map (fun value -> value / columnNorm))

        QrDecompositionResult(q, r)

    static member Eigenvalues(matrix: Matrix2x2) =
        let trace = matrix.M11 + matrix.M22
        let determinant = LinearAlgebraFunctions.Determinant(matrix)
        let discriminant = (trace * trace) - (4.0 * determinant)

        if discriminant >= 0.0 then
            let root = Math.Sqrt(discriminant)
            Eigen2x2Result(ComplexNumber((trace + root) / 2.0, 0.0), ComplexNumber((trace - root) / 2.0, 0.0))
        else
            let imaginary = Math.Sqrt(-discriminant) / 2.0
            Eigen2x2Result(ComplexNumber(trace / 2.0, imaginary), ComplexNumber(trace / 2.0, -imaginary))