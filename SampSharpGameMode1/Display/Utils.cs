using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Display
{
    public class DisplayUtils
    {
        public static string GetKeyString(Keys keys)
        {
            Tuple<string, string> keyString;
            switch(keys)
            {
                case Keys.Action:
                    keyString = new Tuple<string, string>("~k~~PED_ANSWER_PHONE~", "~k~~VEHICLE_FIREWEAPON_ALT~");
                    break;
                case Keys.Aim:
                    keyString = new Tuple<string, string>("~k~~PED_LOCK_TARGET~", "~k~~VEHICLE_HANDBRAKE~");
                    break;
                case Keys.Crouch:
                    keyString = new Tuple<string, string>("~k~~PED_DUCK~", "~k~~VEHICLE_HORN~");
                    break;
                case Keys.CtrlBack:
                    keyString = new Tuple<string, string>("~k~~GROUP_CONTROL_BWD~", "~k~~GROUP_CONTROL_BWD~");
                    break;
                case Keys.Fire:
                    keyString = new Tuple<string, string>("~k~~PED_FIREWEAPON~", "~k~~VEHICLE_FIREWEAPON~");
                    break;
                case Keys.Jump:
                    keyString = new Tuple<string, string>("~k~~PED_JUMPING~", "~k~~VEHICLE_BRAKE~");
                    break;
                case Keys.No:
                    keyString = new Tuple<string, string>("~k~~CONVERSATION_NO~", "~k~~CONVERSATION_NO~");
                    break;
                case Keys.SecondaryAttack:
                    keyString = new Tuple<string, string>("~k~~VEHICLE_ENTER_EXIT~", "~k~~VEHICLE_ENTER_EXIT~");
                    break;
                case Keys.Sprint:
                    keyString = new Tuple<string, string>("~k~~PED_SPRINT~", "~k~~VEHICLE_ACCELERATE~");
                    break;
                case Keys.Submission:
                    keyString = new Tuple<string, string>("~k~~TOGGLE_SUBMISSIONS~", "~k~~TOGGLE_SUBMISSIONS~");
                    break;
                case Keys.Walk:
                    keyString = new Tuple<string, string>("	~k~~SNEAK_ABOUT~", "");
                    break;
                case Keys.Yes:
                    keyString = new Tuple<string, string>("~k~~CONVERSATION_YES~", "~k~~CONVERSATION_YES~");
                    break;
                default:
                    keyString = new Tuple<string, string>("[UNKNOWN]", "[UNKNOWN]");
                    break;
            }
            if(keyString.Item1 == keyString.Item2)
                return $"{ColorPalette.Primary.Main}{keyString.Item1}{Color.White}";
            else
                return $"{ColorPalette.Primary.Main}{keyString.Item1}{Color.White} or {ColorPalette.Primary.Main}{keyString.Item2}{Color.White}";
        }
    }
}
