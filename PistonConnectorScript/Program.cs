using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        /// <summary>
        /// Grupos pendientes de procesar cuando se lanza el evento <see cref="UpdateType.Once"/>.
        /// </summary>
        List<string> _groupPool = new List<string>();
        /// <summary>
        /// Número de veces seguidas que se ha lanzado el evento <see cref="UpdateType.Once"/>.
        /// </summary>
        int _tickIndex = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        /// <summary>
        /// Controla un conjunto formado por pistón + conector.
        /// Si el conector está acoplado, tratará de desacoplarlo y bajar el pistón.
        /// Si el conector está desacoplado, tratará de subir el pistón y, al parar, acoplar el conector.
        /// </summary>
        /// <param name="argument">Nombre del grupo de pistón + conector que se quiere controlar.</param>
        /// <remarks>
        /// Se mostrará información de depuración en un TextPanel.
        /// </remarks>
        public void Main(string argument, UpdateType updateSource)
        {
            var debug = new List<string>();

            if (argument == "" && updateSource == UpdateType.Once)
            {
                _tickIndex++;
                if (_tickIndex >= 60)
                {
                    _tickIndex = 0;
                }
                debug.Add($"Tick index: {_tickIndex}");

                if (_tickIndex % 20 == 0)
                {
                    foreach (var groupName in _groupPool.ToArray())
                    {
                        ControlConnector(groupName, updateSource, debug);
                    }
                }
                else
                {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            }
            else
            {
                ControlConnector(argument, updateSource, debug);
            }
        }

        void ControlConnector(string groupName, UpdateType updateSource, IList<string> debug)
        {
            var blockGroup = GridTerminalSystem.GetBlockGroupWithName(groupName);

            debug.Add($"Group name: '{groupName}'");
            debug.Add($"Update source: {updateSource}");
            if (blockGroup == null)
            {
                // Group does not exists.
                debug.Add($"Group does not exists: '{groupName}'.");
            }
            else
            {
                var piston = blockGroup.GetBlocksOfType<IMyPistonBase>().FirstOrDefault();
                var connector = blockGroup.GetBlocksOfType<IMyShipConnector>().FirstOrDefault();

                _groupPool.Remove(groupName);
                if (piston == null)
                {
                    // Piston not found in group.
                    debug.Add($"No piston detected in group: '{groupName}'.");

                }
                else if (connector == null)
                {
                    // Connector not found in group.
                    debug.Add($"No connector detected in group: '{groupName}'.");
                }
                else
                {
                    debug.Add($"Piston status (before): {piston.Status}");
                    debug.Add($"Connector status (before): {connector.Status}");
                    if ((updateSource & UpdateType.Once) == UpdateType.Once)
                    {
                        // Piston moving.
                        if (piston.Status == PistonStatus.Extended || (connector.Status == MyShipConnectorStatus.Connectable && piston.Status == PistonStatus.Extending))
                        {
                            connector.Connect();
                        }
                        else if (piston.Status == PistonStatus.Retracted)
                        {
                            // Done.
                        }
                        else
                        {
                            _groupPool.Add(groupName);
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                        }
                    }
                    else
                    {
                        // Program triggered.
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            // Disconnect.
                            connector.Disconnect();
                            piston.Retract();
                            _groupPool.Add(groupName);
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                        }
                        else
                        {
                            // Connect.
                            piston.Extend();
                            _groupPool.Add(groupName);
                            Runtime.UpdateFrequency |= UpdateFrequency.Once;
                        }
                    }
                    debug.Add($"Piston status (after): {piston.Status}");
                    debug.Add($"Connector status (after): {connector.Status}");
                }
                PrintDebug(blockGroup, debug);
            }
        }

        /// <summary>
        /// Pinta los registros de depuración en aquellas pantallas que tengan como CustomData "debug".
        /// </summary>
        static void PrintDebug(IMyBlockGroup blockGroup, IList<string> debug)
        {
            var blocks = blockGroup.GetBlocks();
            var textSurfaces = DisplayHelper.GetTextSurfaces(blocks);
            var screens = textSurfaces.Where(x => x.CustomData.Equals("PistonConnector.Debug", StringComparison.OrdinalIgnoreCase));

            foreach (var screen in screens)
            {
                var stringBuilder = new StringBuilder();

                if (screen.TextSurface.ContentType != ContentType.TEXT_AND_IMAGE)
                {
                    screen.TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                }
                foreach (var line in debug)
                {
                    stringBuilder.AppendLine(line);
                }
                screen.TextSurface.WriteText(stringBuilder);
            }
        }

    }
}
