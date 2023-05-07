using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NothingButNeurons.Visualizer.NetworkVisualization;

internal partial class Updater : ActorBase
{
    private void DrawRegionsAndNeurons()
    {
        _networkVisualizationCanvas.Dispatcher.Invoke(() =>
        {
            double margin = 20;
            const double neuronRadius = 10;

            double canvasWidth = _networkVisualizationCanvas.Width;
            double canvasHeight = _networkVisualizationCanvas.Height;

            int regionCount = _regions.Count;

            double regionBaseWidth = neuronRadius * 1.5;
            double regionTotalWidth = 0;
            double regionHeight = canvasHeight - 2 * margin;
            int maxNeuronsPerCol = (int)Math.Floor(regionHeight / regionBaseWidth);
            maxNeuronsPerCol = Math.Max(1, maxNeuronsPerCol % 2 == 0 ? maxNeuronsPerCol - 1 : maxNeuronsPerCol);
            // Shrink it down to size (to evenly distribute max neurons) and adjust margin to match
            double oldHeight = regionHeight;
            regionHeight = maxNeuronsPerCol * regionBaseWidth;
            margin += (oldHeight - regionHeight) / 2;

            foreach (RegionInfo region in _regions.Values)
            {
                regionTotalWidth += regionBaseWidth * Math.Ceiling((float)region.Neurons.Count / maxNeuronsPerCol);
            }

            double regionGapTotal = canvasWidth - 2 * margin - regionTotalWidth;
            // Gap between region types is at least 2x the margin; at most 2.5x the typical region gap (sorta, seeing as typical gap depends on this gap) if that is > 2x margin
            double regionIntertypeGap = Math.Max(margin * 2, regionGapTotal / (_regions.Count - 1) * 2.5);
            double intertypeGaps = hasInteriorRegions ? 2 : 1;
            double regionGap = Math.Max(neuronRadius, (regionGapTotal - intertypeGaps * regionIntertypeGap) / (_regions.Count - 1 - intertypeGaps));

            if (regionIntertypeGap <= margin * 2 || regionGap <= neuronRadius)
                SendDebugMessage(DebugSeverity.Warning, "NetworkVisualization", "Canvas too small / too many region/neurons - visualization will scroll horizontally");

            double offset = margin;
            int regionIndex = 0;
            RegionType? lastType = null;
            foreach (RegionInfo region in _regions.Values.OrderBy(r => r.Address))
            {
                double gap = lastType == null ? 0 : lastType == region.Type ? regionGap : regionIntertypeGap;
                offset += gap;
                lastType = region.Type;
                double regionX = offset;
                double width = regionBaseWidth * Math.Ceiling((float)region.Neurons.Count / maxNeuronsPerCol);
                offset += width;

                double neurColCt = Math.Ceiling((float)region.Neurons.Count / maxNeuronsPerCol);
                int neurPerCol = (int)Math.Floor(region.Neurons.Count / neurColCt);
                double availableRegionHeight = regionHeight - 2 * neuronRadius * 1.5;

                Rectangle regionRect = new Rectangle
                {
                    Width = width,
                    Height = regionHeight,
                    Stroke = Brushes.Black,
                    Fill = region.Type switch
                    {
                        RegionType.Input => new SolidColorBrush(Color.FromArgb(128, 173, 216, 230)), // Pale LightBlue
                        RegionType.Interior => new SolidColorBrush(Color.FromArgb(128, 144, 238, 144)), // Pale LightGreen
                        RegionType.Output => new SolidColorBrush(Color.FromArgb(128, 240, 128, 128)), // Pale LightCoral
                        _ => throw new ArgumentOutOfRangeException()
                    }
                };

                Canvas.SetLeft(regionRect, regionX);
                Canvas.SetTop(regionRect, margin);
                Panel.SetZIndex(regionRect, 0);
                _networkVisualizationCanvas.Children.Add(regionRect);

                int neuronCount = region.Neurons.Count;
                int neuronIndex = 0;
                foreach (NeuronInfo neuron in region.Neurons.OrderBy(n => n.Address))
                {
                    int neuronGridX = Math.DivRem(neuronIndex, neurPerCol, out int neuronGridY);
                    int neurThisCol = neurPerCol;
                    if (neuronGridX >= neurColCt - 1 && neurColCt > 1)
                    {
                        neurThisCol = (int)(neuronCount - (neurColCt - 1) * neurPerCol);
                    }
                    if (neuronGridX > neurColCt - 1)
                    {
                        neuronGridX--;
                        neuronGridY += neurPerCol;
                    }

                    double neuronX = regionX + regionBaseWidth / 2 + neuronGridX * regionBaseWidth;

                    double neuronY;
                    if (neurThisCol == 1)
                    {
                        neuronY = margin + regionHeight / 2;
                    }
                    else
                    {
                        double totalGap = availableRegionHeight - (neurThisCol * neuronRadius);
                        double spacingBetweenNeurons = totalGap / (neurThisCol - 1);
                        neuronY = margin + neuronRadius * 2 + neuronGridY * (spacingBetweenNeurons + neuronRadius);
                    }

                    // Store the calculated position in the NeuronInfo object
                    neuron.Position = new Point(neuronX, neuronY);

                    Ellipse neuronEllipse = new Ellipse
                    {
                        Width = neuronRadius,
                        Height = neuronRadius,
                        Stroke = Brushes.Black,
                        Fill = Brushes.White
                    };

                    //Debug.WriteLine($"Adding neuron {neuron.Id} at : {neuronX - neuronEllipse.Width / 2},{neuronY - neuronEllipse.Height / 2}");
                    Canvas.SetLeft(neuronEllipse, neuronX - neuronEllipse.Width / 2);
                    Canvas.SetTop(neuronEllipse, neuronY - neuronEllipse.Height / 2);
                    Panel.SetZIndex(neuronEllipse, 3);
                    _networkVisualizationCanvas.Children.Add(neuronEllipse);
                    _neuronEllipses[neuron.Id] = neuronEllipse;

                    neuronIndex++;
                }

                regionIndex++;
            }

            foreach (ConnectionInfo connection in _connections.Values)
            {
                NeuronInfo sourceNeuron = connection.SourceNeuron;
                NeuronInfo targetNeuron = connection.TargetNeuron;

                (Path connectionPath, Polygon[] arrowheads) = CreateConnectionPath(sourceNeuron.Position, targetNeuron.Position, connection.Id);

                _connectionPaths[connection.Id] = connectionPath;
                Panel.SetZIndex(connectionPath, 1);
                _networkVisualizationCanvas.Children.Add(connectionPath);
                foreach (Polygon arrowhead in arrowheads)
                {
                    Panel.SetZIndex(arrowhead, 2);
                    _networkVisualizationCanvas.Children.Add(arrowhead);
                }
            }
        });
    }

    private void UpdateNeuron(string neuronPid, double signalBuffer)
    {
        if (_neurons.ContainsKey(neuronPid))
        {
            _neuronUpdates[neuronPid] = signalBuffer;
        }
    }

    private void UpdateConnection(string connectionId, double signalStrength)
    {
        if (_connections.ContainsKey(connectionId))
        {
            _connectionUpdates[connectionId] = signalStrength;
        }
    }

    private void ApplyNeuronUpdates()
    {
        foreach (var entry in _neuronUpdates)
        {
            string neuronPid = entry.Key;
            double signalBuffer = entry.Value;

            if (_neurons.TryGetValue(neuronPid, out NeuronInfo neuron) &&
                _neuronEllipses.TryGetValue(neuron.Id, out Ellipse neuronEllipse))
            {
                SolidColorBrush newColor = GetSignalOrBufferColor(signalBuffer, 1.333d);
                neuronEllipse.Fill = newColor;
            }
        }
    }

    private void ApplyConnectionUpdates()
    {
        lock (connectionLock)
        {
            foreach (var entry in _connectionUpdates)
            {
                if (entry.Key == null)
                    continue;

                string connectionId = entry.Key;
                double signalStrength = entry.Value;

                if (_connections.TryGetValue(connectionId, out ConnectionInfo connection) &&
                    _connectionPaths.TryGetValue(connectionId, out Path connectionPath))
                {
                    SolidColorBrush newColor = GetSignalOrBufferColor(signalStrength, 6.66d);
                    connectionPath.Stroke = newColor;
                    connection.Timer.Stop();
                    connection.Timer.Start();
                }
            }
        }
    }

    private void ResetPathColor(string connectionId)
    {
        if (_connectionPaths.TryGetValue(connectionId, out Path connectionPath))
        {
            /*_networkVisualizationCanvas.Dispatcher.Invoke(() =>
            {
                connectionPath.Stroke = color;
            });*/
            lock (connectionLock)
            {
                _connectionUpdates[connectionId] = 0;
            }
        }
    }

    private (Path, Polygon[]) CreateConnectionPath(Point source, Point target, string connectionId)
    {
        PathFigure pathFigure = new PathFigure
        {
            StartPoint = source,
            IsClosed = false
        };

        double dx = target.X - source.X;
        double dy = target.Y - source.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        bool isSelfConnection = distance == 0;
        double controlPointOffset = isSelfConnection ? 75 : distance * 0.3;

        // Introduce randomness to the control point offsets
        double randomFactor = 0.4;
        double controlPointOffset1 = controlPointOffset * (1 + randomFactor * (NextDoubleInRange(0.25f, 0.6f) * 2 - 1));
        double controlPointOffset2 = controlPointOffset * (1 + randomFactor * (NextDoubleInRange(0.4f, 0.75f) * 2 - 1));
        while (Math.Abs(controlPointOffset2 - controlPointOffset1) <= 0.2f)
        {
            controlPointOffset2 = controlPointOffset * (1 + randomFactor * (NextDoubleInRange(0.35f, 0.75f) * 2 - 1));
        }

        double controlPoint1XOffset, controlPoint1YOffset, controlPoint2XOffset, controlPoint2YOffset;
        if (isSelfConnection)
        {
            controlPoint1XOffset = controlPointOffset1;
            controlPoint1YOffset = -controlPointOffset1 / 2;
            controlPoint2XOffset = controlPointOffset1;
            controlPoint2YOffset = controlPointOffset1 / 2;
        }
        else
        {
            controlPoint1XOffset = controlPointOffset1 * dy / distance;
            controlPoint1YOffset = controlPointOffset1 * dx / distance;
            controlPoint2XOffset = controlPointOffset2 * dy / distance;
            controlPoint2YOffset = controlPointOffset2 * dx / distance;
        }

        bool isControlPoint1XPositive = _rng.Next(0, 2) == 0;
        bool isControlPoint1YPositive = _rng.Next(0, 2) == 0;
        Point controlPoint1 = new Point(source.X + (isControlPoint1XPositive ? controlPoint1XOffset : -controlPoint1XOffset), source.Y - (isControlPoint1YPositive ? -controlPoint1YOffset : controlPoint1YOffset));
        Point controlPoint2 = new Point(target.X + (isControlPoint1XPositive ? -controlPoint2XOffset : controlPoint2XOffset), target.Y - (isControlPoint1YPositive ? controlPoint2YOffset : -controlPoint2YOffset));

        BezierSegment bezierSegment = new BezierSegment(controlPoint1, controlPoint2, target, true);
        pathFigure.Segments.Add(bezierSegment);

        PathGeometry pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        Path connectionPath = new Path
        {
            Data = pathGeometry,
            StrokeThickness = 1,
            Stroke = Brushes.Black
        };

        // Define the t values for the arrowheads
        double pathLen = CalculateBezierLength(pathGeometry);
        double t1 = Math.Min(0.1666d, Math.Max(0.03666d, (0.003d * Math.Pow(pathLen, 2d) - 3.9514d * pathLen + 1571d) / 10000d));
        //Debug.WriteLine($"{pathLen}: {t1}");
        double[] tValues = { t1, 1 - t1 };

        // Create an array to store the arrowheads
        Polygon[] arrowheads = new Polygon[tValues.Length];
        double arrowheadWidth = 5;
        double arrowheadHeight = 6;

        for (int i = 0; i < tValues.Length; i++)
        {
            double t = tValues[i];
            
            pathGeometry.GetPointAtFractionLength(t, out Point arrowheadPosition, out Point tangent);
            double angle = Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI;

            // Create the arrowhead
            Polygon arrowhead = new Polygon
            {
                Fill = Brushes.Black,
                Points = new PointCollection(new List<Point>
                {
                    new Point(0, 0),
                    new Point(-arrowheadWidth, -(arrowheadHeight/2)),
                    new Point(-arrowheadWidth, arrowheadHeight/2)
                })
            };

            Point arrowheadCentroid = CalculateTriangleCentroid(arrowhead.Points[0], arrowhead.Points[1], arrowhead.Points[2]);

            // Position the arrowhead centered at the arrowheadPosition
            arrowhead.SetValue(Canvas.LeftProperty, arrowheadPosition.X - arrowheadCentroid.X);
            arrowhead.SetValue(Canvas.TopProperty, arrowheadPosition.Y - arrowheadCentroid.Y);

            // Rotate the arrowhead to align with the path
            RotateTransform rotateTransform = new RotateTransform(angle, arrowheadCentroid.X, arrowheadCentroid.Y);
            arrowhead.RenderTransform = rotateTransform;

            // Add the arrowhead to the array
            arrowheads[i] = arrowhead;
        }

        // Return the connectionPath and array of arrowheads
        return (connectionPath, arrowheads);
    }

    private double CalculateBezierLength(PathGeometry pathGeometry, double tolerance = 0.1)
    {
        PathGeometry flattenedPathGeometry = pathGeometry.GetFlattenedPathGeometry(tolerance, ToleranceType.Relative);
        double length = 0.0;

        foreach (PathFigure figure in flattenedPathGeometry.Figures)
        {
            Point startPoint = figure.StartPoint;
            Point previousPoint = startPoint;

            foreach (PathSegment segment in figure.Segments)
            {
                if (segment is PolyLineSegment polyLineSegment)
                {
                    foreach (Point point in polyLineSegment.Points)
                    {
                        double dx = point.X - previousPoint.X;
                        double dy = point.Y - previousPoint.Y;
                        double segmentLength = Math.Sqrt(dx * dx + dy * dy);

                        length += segmentLength;
                        previousPoint = point;
                    }
                }
            }
        }

        return length;
    }

    private Point CalculateTriangleCentroid(Point p1, Point p2, Point p3)
    {
        double centroidX = (p1.X + p2.X + p3.X) / 3;
        double centroidY = (p1.Y + p2.Y + p3.Y) / 3;

        return new Point(centroidX, centroidY);
    }

    private double NextDoubleInRange(double minValue, double maxValue)
    {
        return minValue + _rng.NextDouble() * (maxValue - minValue);
    }

    private SolidColorBrush GetSignalOrBufferColor(double str, double amplificationFactor)
    {
        double normalizedStrength = Math.Clamp(str, -1, 1);

        Color color = Color.FromRgb(
            (byte)(normalizedStrength < 0 ? 255 * -normalizedStrength * amplificationFactor : 0),
            (byte)(normalizedStrength > 0 ? 255 * normalizedStrength * amplificationFactor : 0),
            0
        );

        return new SolidColorBrush(color);
    }
}