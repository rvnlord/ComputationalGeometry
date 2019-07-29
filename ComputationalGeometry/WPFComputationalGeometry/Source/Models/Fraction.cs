using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WPFComputationalGeometry.Source.Models
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Fraction : IComparable, IFormattable
    {
        private long _numerator;
        private long _denominator;

        public long Numerator
        {
            get { return _numerator; }
            set { _numerator = value; }
        }

        public long Denominator
        {
            get { return _denominator; }
            set { _denominator = value; }
        }

        public static readonly Fraction NaN = new Fraction(Indeterminates.NaN);
        public static readonly Fraction PositiveInfinity = new Fraction(Indeterminates.PositiveInfinity);
        public static readonly Fraction NegativeInfinity = new Fraction(Indeterminates.NegativeInfinity);
        public static readonly Fraction Zero = new Fraction(0, 1);
        public static readonly Fraction Epsilon = new Fraction(1, long.MaxValue);
        private static readonly double EpsilonDouble = 1.0 / long.MaxValue;
        public static readonly Fraction MaxValue = new Fraction(long.MaxValue, 1);
        public static readonly Fraction MinValue = new Fraction(long.MinValue, 1);

        public Fraction(long wholeNumber)
        {
            if (wholeNumber == long.MinValue)
                wholeNumber++;

            _numerator = wholeNumber;
            _denominator = 1;
        }
        
        public Fraction(double floatingPointNumber)
        {
            this = ToFraction(floatingPointNumber);
        }
        
        public Fraction(string inValue)
        {
            this = ToFraction(inValue);
        }
        
        public Fraction(long numerator, long denominator)
        {
            if (numerator == long.MinValue)
                numerator++;

            if (denominator == long.MinValue)
                denominator++;  

            _numerator = numerator;
            _denominator = denominator;
            ReduceFraction(ref this);
        }
        
        private Fraction(Indeterminates type)
        {
            _numerator = (long)type;
            _denominator = 0;
        }
        
        public int ToInt32()
        {
            if (_denominator == 0)
                throw new FractionException($"Nie można przekonwertować {IndeterminateTypeName(_numerator)} do Int32", new NotFiniteNumberException());

            long bestGuess = _numerator / _denominator;

            if (bestGuess > int.MaxValue || bestGuess < int.MinValue)
                throw new FractionException("Cannot convert to Int32", new System.OverflowException());

            return (int)bestGuess;
        }
        
        public long ToInt64()
        {
            if (_denominator == 0)
                throw new FractionException($"Nie można przekonwertować {IndeterminateTypeName(_numerator)} do Int64", new NotFiniteNumberException());

            return _numerator / _denominator;
        }
        
        public double ToDouble()
        {
            if (_denominator == 1)
                return _numerator;
            if (_denominator == 0)
            {
                switch (NormalizeIndeterminate(_numerator))
                {
                    case Indeterminates.NegativeInfinity:
                        return double.NegativeInfinity;

                    case Indeterminates.PositiveInfinity:
                        return double.PositiveInfinity;

                    default:
                        return double.NaN;
                }
            }
            return _numerator / (double)_denominator;
        }

        public override string ToString()
        {
            return $"{ToDouble():0.00}";
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return $"{ToDouble():0.00}";
        }

        //public override string ToString()
        //{
        //    var sb = new StringBuilder();
        //    var divided = Numerator / Denominator;
        //    var mod = Numerator % Denominator;

        //    if (divided == 0 && mod == 0)
        //        return "0";
        //    if (divided != 0)
        //        sb.Append(divided);
        //    if (divided != 0 && mod != 0)
        //        sb.Append(", ");
        //    if (mod != 0)
        //        sb.Append($"{mod}/{Denominator}");

        //    var result = sb.ToString();
        //    if (result.Contains("-"))
        //        result = "-" + result.Replace("-", "");

        //    return result;
        //}

        //string IFormattable.ToString(string format, IFormatProvider formatProvider)
        //{
        //    var sb = new StringBuilder();
        //    var divided = Numerator / Denominator;
        //    var mod = Numerator % Denominator;

        //    if (divided == 0 && mod == 0)
        //        return "0";
        //    if (divided != 0)
        //        sb.Append(divided.ToString(format, formatProvider));
        //    if (divided != 0 && mod != 0)
        //        sb.Append(", ");
        //    if (mod != 0)
        //        sb.Append($"{mod.ToString(format, formatProvider)}/{Denominator.ToString(format, formatProvider)}");

        //    var result = sb.ToString();
        //    if (result.Contains("-"))
        //        result = "-" + result.Replace("-", "");

        //    return result;
        //}

        public static Fraction ToFraction(long inValue)
        {
            return new Fraction(inValue);
        }
        
        public static Fraction ToFraction(double inValue)
        {
            if (double.IsNaN(inValue))
                return NaN;
            if (double.IsNegativeInfinity(inValue))
                return NegativeInfinity;
            if (double.IsPositiveInfinity(inValue))
                return PositiveInfinity;
            if (Math.Abs(inValue) < 0.00001d)
                return Zero;

            if (inValue > int.MaxValue)
                throw new OverflowException($"Wartość double {inValue} jest zbyt duża");
            if (inValue < -int.MaxValue)
                throw new OverflowException($"Wartość double {inValue} jest zbyt mała");
            if (-EpsilonDouble < inValue && inValue < EpsilonDouble)
                throw new ArithmeticException($"Double {inValue} cannot be represented");

            var sign = Math.Sign(inValue);
            inValue = Math.Abs(inValue);

            return ConvertPositiveDouble(sign, inValue);
        }
        
        public static Fraction ToFraction(string inValue)
        {
            if (string.IsNullOrEmpty(inValue))
                throw new ArgumentNullException(nameof(inValue));
            
            var info = NumberFormatInfo.CurrentInfo;
            var trimmedValue = inValue.Trim();

            if (trimmedValue == info.NaNSymbol) // Sprawdź czy jest specjalnym symbolem
                return NaN;
            if (trimmedValue == info.PositiveInfinitySymbol)
                return PositiveInfinity;
            if (trimmedValue == info.NegativeInfinitySymbol)
                return NegativeInfinity;
            
            var slashPos = inValue.IndexOf('/'); // Sprawdź czy jest ułamkiem

            if (slashPos > -1) // Jeśli true, to string ma format Licznik / Mianownik
            {
                var numerator = Convert.ToInt64(inValue.Substring(0, slashPos));
                var denominator = Convert.ToInt64(inValue.Substring(slashPos + 1));

                return new Fraction(numerator, denominator);
            }
            
            var decimalPos = inValue.IndexOf(info.CurrencyDecimalSeparator, StringComparison.Ordinal); // string nie ma formatu ma format Licznik / Mianownik, sprawdź czy jest to int, decimal lub double

            return decimalPos > -1 
                ? new Fraction(Convert.ToDouble(inValue)) 
                : new Fraction(Convert.ToInt64(inValue));
        }
        
        public bool IsNaN()
        {
            return _denominator == 0 && NormalizeIndeterminate(_numerator) == Indeterminates.NaN;
        }

        public bool IsInfinity()
        {
            return _denominator == 0 && NormalizeIndeterminate(_numerator) != Indeterminates.NaN;
        }
        
        public bool IsPositiveInfinity()
        {
            return _denominator == 0 && NormalizeIndeterminate(_numerator) == Indeterminates.PositiveInfinity;
        }
        
        public bool IsNegativeInfinity()
        {
            return _denominator == 0 && NormalizeIndeterminate(_numerator) == Indeterminates.NegativeInfinity;
        }
        
        public Fraction Inverse()
        {
            return new Fraction // Nie ma użytego konstruktora Licznik / Mianownik, ponieważ normalizacja nie jest w wtym wypadku porządana
            {
                Numerator = _denominator,
                Denominator = _numerator
            };
        }
        
        public static Fraction Inverse(long value)
        {
            return new Fraction(value).Inverse();
        }
        
        public static Fraction Inverted(double value)
        {
            return new Fraction(value).Inverse();
        }

        public Fraction Abs()
        {
            if (IsNaN())
                return NaN;
            return IsInfinity() 
                ? PositiveInfinity 
                : new Fraction(Math.Abs(_numerator), Math.Abs(_denominator));
        }

        public static Fraction operator -(Fraction left)
        {
            return Negate(left);
        }

        public static Fraction operator +(Fraction left, Fraction right)
        {
            return Add(left, right);
        }

        public static Fraction operator +(long left, Fraction right)
        {
            return Add(new Fraction(left), right);
        }

        public static Fraction operator +(Fraction left, long right)
        {
            return Add(left, new Fraction(right));
        }

        public static Fraction operator +(double left, Fraction right)
        {
            return Add(ToFraction(left), right);
        }

        public static Fraction operator +(Fraction left, double right)
        {
            return Add(left, ToFraction(right));
        }

        public static Fraction operator -(Fraction left, Fraction right)
        {
            return Add(left, -right);
        }

        public static Fraction operator -(long left, Fraction right)
        {
            return Add(new Fraction(left), -right);
        }

        public static Fraction operator -(Fraction left, long right)
        {
            return Add(left, new Fraction(-right));
        }

        public static Fraction operator -(double left, Fraction right)
        {
            return Add(ToFraction(left), -right);
        }

        public static Fraction operator -(Fraction left, double right)
        {
            return Add(left, ToFraction(-right));
        }

        public static Fraction operator *(Fraction left, Fraction right)
        {
            return Multiply(left, right);
        }

        public static Fraction operator *(long left, Fraction right)
        {
            return Multiply(new Fraction(left), right);
        }

        public static Fraction operator *(Fraction left, long right)
        {
            return Multiply(left, new Fraction(right));
        }

        public static Fraction operator *(double left, Fraction right)
        {
            return Multiply(ToFraction(left), right);
        }

        public static Fraction operator *(Fraction left, double right)
        {
            return Multiply(left, ToFraction(right));
        }

        public static Fraction operator /(Fraction left, Fraction right)
        {
            return Multiply(left, right.Inverse());
        }

        public static Fraction operator /(long left, Fraction right)
        {
            return Multiply(new Fraction(left), right.Inverse());
        }

        public static Fraction operator /(Fraction left, long right)
        {
            return Multiply(left, Inverted(right));
        }

        public static Fraction operator /(double left, Fraction right)
        {
            return Multiply(ToFraction(left), right.Inverse());
        }

        public static Fraction operator /(Fraction left, double right)
        {
            return Multiply(left, Inverted(right));
        }

        public static Fraction operator %(Fraction left, Fraction right)
        {
            return Modulus(left, right);
        }

        public static Fraction operator %(long left, Fraction right)
        {
            return Modulus(new Fraction(left), right);
        }

        public static Fraction operator %(Fraction left, long right)
        {
            return Modulus(left, right);
        }

        public static Fraction operator %(double left, Fraction right)
        {
            return Modulus(ToFraction(left), right);
        }

        public static Fraction operator %(Fraction left, double right)
        {
            return Modulus(left, right);
        }

        public static bool operator ==(Fraction left, Fraction right)
        {
            return left.CompareEquality(right, false);
        }

        public static bool operator ==(Fraction left, long right)
        {
            return left.CompareEquality(new Fraction(right), false);
        }

        public static bool operator ==(Fraction left, double right)
        {
            return left.CompareEquality(new Fraction(right), false);
        }

        public static bool operator !=(Fraction left, Fraction right)
        {
            return left.CompareEquality(right, true);
        }

        public static bool operator !=(Fraction left, long right)
        {
            return left.CompareEquality(new Fraction(right), true);
        }

        public static bool operator !=(Fraction left, double right)
        {
            return left.CompareEquality(new Fraction(right), true);
        }

        public static bool operator <(Fraction left, Fraction right)
        {
            return left.CompareTo(right) < 0;
        }
        
        public static bool operator >(Fraction left, Fraction right)
        {
            return left.CompareTo(right) > 0;
        }
        
        public static bool operator <=(Fraction left, Fraction right)
        {
            return left.CompareTo(right) <= 0;
        }
        
        public static bool operator >=(Fraction left, Fraction right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static implicit operator Fraction(long value)
        {
            return new Fraction(value);
        }
        
        public static implicit operator Fraction(double value)
        {
            return new Fraction(value);
        }
        
        public static implicit operator Fraction(string value)
        {
            return new Fraction(value);
        }

        public static explicit operator int(Fraction frac)
        {
            return frac.ToInt32();
        }
        
        public static explicit operator long(Fraction frac)
        {
            return frac.ToInt64();
        }
        
        public static explicit operator double(Fraction frac)
        {
            return frac.ToDouble();
        }
        
        public static implicit operator string(Fraction frac)
        {
            return frac.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fraction))
                return false;

            try
            {
                var right = (Fraction)obj;
                return CompareEquality(right, false);
            }
            catch
            {
                return false;
            }
        }
        
        public override int GetHashCode()
        {
            ReduceFraction(ref this);

            var numeratorHash = _numerator.GetHashCode();
            var denominatorHash = _denominator.GetHashCode();

            return numeratorHash ^ denominatorHash;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1; // wartość null jest mniejsza niż wszystkie inne

            Fraction right;

            if (obj is Fraction)
                right = (Fraction)obj;
            else if (obj is long)
                right = (long)obj;
            else if (obj is double)
                right = (double)obj;
            else if (obj is string)
                right = (string)obj;
            else
                throw new ArgumentException(@"Obiekt nie jest konwertowalny do Ułamka", nameof(obj));

            return CompareTo(right);
        }

        //public int CompareTo(Fraction right) // Nie można robić CrossReduce Tutaj, bo skala będzie niewłaściwa
        //{
        //    if (_denominator == 0)
        //        return IndeterminantCompare(NormalizeIndeterminate(_numerator), right);

        //    if (right.Denominator == 0)
        //        return -IndeterminantCompare(NormalizeIndeterminate(right.Numerator), this);

        //    ReduceFraction(ref this);
        //    ReduceFraction(ref right);

        //    try
        //    {
        //        checked
        //        {
        //            BigInteger leftScale = _numerator * right.Denominator;
        //            BigInteger rightScale = _denominator * right.Numerator;

        //            if (leftScale < rightScale)
        //                return -1;
        //            return leftScale > rightScale ? 1 : 0;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        //throw new FractionException($"Metoda CompareTo({this}, {right}) zwróciła Error", e);
        //        var d1 = this.ToDouble();
        //        var d2 = right.ToDouble();

        //        if (d1 > d2)
        //            return 1;
        //        if (d1 < d2)
        //            return -1;
        //        return 0;
        //    }
        //}

        public int CompareTo(Fraction right)
        {
            if (_denominator == 0)
                return IndeterminantCompare(NormalizeIndeterminate(_numerator), right);

            if (right.Denominator == 0)
                return -IndeterminantCompare(NormalizeIndeterminate(right.Numerator), this);

            ReduceFraction(ref this);
            ReduceFraction(ref right);

            var fr1 = new Fraction(_numerator, _denominator);
            var fr2 = new Fraction(right.Numerator, right.Denominator);

            var result = CompareRecursive(fr1, fr2);

            //string sign;
            //if (result > 0)
            //    sign = ">";
            //else if (result < 0)
            //    sign = "<";
            //else
            //    sign = "=";

            //Debug.Print($"{fr1} ({fr1.ToDouble():0.00}) {sign} ({fr2.ToDouble():0.00}) {fr2}");

            return result;
        }

        private static int CompareRecursive(Fraction fr1, Fraction fr2)
        {
            while (true)
            {
                var qFr1 = fr1.Numerator / fr1.Denominator;
                var qFr2 = fr2.Numerator / fr2.Denominator;
                var rFr1 = fr1.Numerator % fr1.Denominator;
                var rFr2 = fr2.Numerator % fr2.Denominator;

                if (qFr1 == qFr2)
                {
                    if (rFr1 == 0 && rFr2 == 0)
                        return 0;
                    if (rFr1 == 0)
                        return rFr2 > 0 ? -1 : 1;
                    if (rFr2 == 0)
                        return rFr1 > 0 ? 1 : -1;
                    if (fr1.Numerator < fr1.Denominator && fr2.Numerator < fr2.Denominator)
                    {
                        if (fr2.Numerator / fr1.Numerator == fr2.Denominator / fr1.Denominator && fr2.Numerator % fr1.Numerator == 0 && fr2.Denominator % fr1.Denominator == 0)
                            return 0;
                        return ((rFr1 > 0 && rFr2 > 0) || (rFr1 < 0 && rFr2 < 0) ? -1 : 1)
                            * CompareRecursive(new Fraction(fr1.Denominator, rFr1), new Fraction(fr2.Denominator, rFr2)); // Odwróć i kontynuuj
                    }
                }

                if (qFr1 > qFr2)
                    return 1;
                if (qFr1 < qFr2)
                    return -1;

                fr1 = new Fraction(rFr1, fr1.Denominator);
                fr2 = new Fraction(rFr2, fr2.Denominator);
            }
        }

        public static void ReduceFraction(ref Fraction frac)
        {
            if (frac.Denominator == 0) // Wyczyść specjalne przypadki
            {
                frac.Numerator = (long)NormalizeIndeterminate(frac.Numerator);
                return;
            }
            
            if (frac.Numerator == 0) // Wszystkie rodzaje zera sa równoważne
            {
                frac.Denominator = 1;
                return;
            }

            var iGCD = GCD(frac.Numerator, frac.Denominator);
            frac.Numerator /= iGCD;
            frac.Denominator /= iGCD;
            
            if (frac.Denominator < 0) // Przenieś znak negacji do licznika
            {
                frac.Numerator = -frac.Numerator;
                frac.Denominator = -frac.Denominator;
            }
        }
        
        public static void CrossReducePair(ref Fraction frac1, ref Fraction frac2)
        {
            if (frac1.Denominator == 0 || frac2.Denominator == 0)
                return;

            var gcdTop = GCD(frac1.Numerator, frac2.Denominator);
            frac1.Numerator /= gcdTop;
            frac2.Denominator /= gcdTop;

            var gcdBottom = GCD(frac1.Denominator, frac2.Numerator);
            frac2.Numerator /= gcdBottom;
            frac1.Denominator /= gcdBottom;
        }

        private static Fraction ConvertPositiveDouble(int sign, double inValue)
        {
            var fractionNumerator = (long)inValue;
            double fractionDenominator = 1;
            double previousDenominator = 0;
            var remainingDigits = inValue;
            var maxIterations = 594;

            while (remainingDigits != Math.Floor(remainingDigits) && Math.Abs(inValue - fractionNumerator / fractionDenominator) > double.Epsilon)
            {
                remainingDigits = 1.0 / (remainingDigits - Math.Floor(remainingDigits));

                var scratch = fractionDenominator;

                fractionDenominator = (Math.Floor(remainingDigits) * fractionDenominator) + previousDenominator;
                fractionNumerator = (long)(inValue * fractionDenominator + 0.5);

                previousDenominator = scratch;

                if (maxIterations-- < 0)
                    break;
            }

            return new Fraction(fractionNumerator * sign, (long)fractionDenominator);
        }

        private bool CompareEquality(Fraction right, bool notEqualCheck)
        {
            ReduceFraction(ref this); // Najpierw normalizuj
            ReduceFraction(ref right);

            if (_numerator == right.Numerator && _denominator == right.Denominator)
            {
                if (notEqualCheck && IsNaN()) // Specjalny przypadek, dwie wartości NaN są równe
                    return true;
                return !notEqualCheck;
            }
            return notEqualCheck;
        }

        private static int IndeterminantCompare(Indeterminates leftType, Fraction right)
        {
            switch (leftType)
            {
                case Indeterminates.NaN: // NaN jest:
                    if (right.IsNaN()) // równe NaN
                        return 0;
                    if (right.IsNegativeInfinity())
                        return 1; // większe niż -niekończoność
                    return -1; // mniejsze niż cokolwiek innego

                case Indeterminates.NegativeInfinity: // -nieskończoność jest:
                    if (right.IsNegativeInfinity()) // równe -nieskończoność
                        return 0; 
                    return -1; // mniejsze niż cokolwiek innego

                case Indeterminates.PositiveInfinity: // nieskończoność jest:
                    return right.IsPositiveInfinity() ? 0 : 1; // równe nieskończoność lub większe niż cokolwiek innego

                default: // nie powinno być osiągalne
                    return 0;
            }
        }

        private static Fraction Negate(Fraction frac)
        {
            return new Fraction(-frac.Numerator, frac.Denominator); // NaN jest wciąż NaNem
        }
        
        private static Fraction Add(Fraction left, Fraction right)
        {
            if (left.IsNaN() || right.IsNaN())
                return NaN;

            var gcd = GCD(left.Denominator, right.Denominator); // nie może zwrócić mniej niż 1
            var leftDenominator = left.Denominator / gcd;
            var rightDenominator = right.Denominator / gcd;

            try
            {
                checked
                {
                    var numerator = left.Numerator * rightDenominator + right.Numerator * leftDenominator;
                    var denominator = leftDenominator * rightDenominator * gcd;

                    return new Fraction(numerator, denominator);
                }
            }
            catch (Exception)
            {
                //throw new FractionException("Error dodawania", ex);
                return new Fraction(left.ToDouble() + right.ToDouble()); // przybliż ekstremalne przypadki
            }
        }
        
        private static Fraction Multiply(Fraction left, Fraction right)
        {
            if (left.IsNaN() || right.IsNaN())
                return NaN;
            
            CrossReducePair(ref left, ref right);

            try
            {
                checked
                {
                    var numerator = left.Numerator * right.Numerator;
                    var denominator = left.Denominator * right.Denominator;

                    return new Fraction(numerator, denominator);
                }
            }
            catch (Exception)
            {
                //throw new FractionException("Multiply error", ex);
                return new Fraction(left.ToDouble() * right.ToDouble());
            }
        }
        
        private static Fraction Modulus(Fraction left, Fraction right)
        {
            if (left.IsNaN() || right.IsNaN())
                return NaN;

            try
            {
                checked
                {
                    var quotient = (int)(left / right);
                    var whole = new Fraction(quotient * right.Numerator, right.Denominator);
                    return left - whole;
                }
            }
            catch (Exception)
            {
                //throw new FractionException("Modulus error", ex);
                return new Fraction(left.ToDouble() % right.ToDouble());
            }
        }
        
        private static long GCD(long left, long right)
        {
            if (left < 0)
                left = -left;

            if (right < 0)
                right = -right;
            
            if (left < 2 || right < 2)
                return 1;

            do
            {
                if (left < right)
                {
                    var temp = left;  // zamień operandy
                    left = right;
                    right = temp;
                }

                left %= right;
            } while (left != 0);

            return right;
        }

        public static Fraction Atan2(Fraction y, Fraction x)
        {
            var coeff_1 = new Fraction(Math.PI / 4d);
            var coeff_2 = 3 * coeff_1;
            var abs_y = y.Abs();
            Fraction angle;
            if (x >= 0)
            {
                var r = (x - abs_y) / (x + abs_y);
                angle = coeff_1 - coeff_1 * r;
            }
            else {
                var r = (x + abs_y) / (abs_y - x);
                angle = coeff_2 - coeff_1 * r;
            }
            return y < 0d ? -angle : angle;
        }
        
        private static string IndeterminateTypeName(long numerator)
        {
            var info = NumberFormatInfo.CurrentInfo;

            switch (NormalizeIndeterminate(numerator))
            {
                case Indeterminates.PositiveInfinity:
                    return info.PositiveInfinitySymbol;

                case Indeterminates.NegativeInfinity:
                    return info.NegativeInfinitySymbol;

                default:
                    return info.NaNSymbol;
            }
        }
        
        private static Indeterminates NormalizeIndeterminate(long numerator)
        {
            switch (Math.Sign(numerator))
            {
                case 1:
                    return Indeterminates.PositiveInfinity;

                case -1:
                    return Indeterminates.NegativeInfinity;

                default:  
                    return Indeterminates.NaN;
            }
        }
        
        private enum Indeterminates
        {
            NaN = 0,
            PositiveInfinity = 1,
            NegativeInfinity = -1
        }
       
    }   
}
