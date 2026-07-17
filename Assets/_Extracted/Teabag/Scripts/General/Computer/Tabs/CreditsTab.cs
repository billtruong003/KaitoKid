using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CreditsTab : IComputerTab
{
    //[TextArea(0, 10)]
    //public string credits;
    public List<Credit> credits = new List<Credit>();


    public override void OnOpen()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Thanks to:");

        foreach (Credit credit in credits)
        {
            builder.AppendLine(credit.ToString());
        }

        computer.RenderText(builder.ToString());
    }

    public override void OnClose()
    {

    }

    public override void Press(KeyCode key)
    {

    }

    public enum Role
    {
        Creator,
        Developer,
        Artist,
        Musician
    }

    [System.Serializable]
    public class Credit
    {
        public string name;
        public List<Role> roles;

        public override string ToString()
        {
            /*
            List<string> r = new List<string>();
            foreach (Role role in roles)
                r.Add(role.ToString());'
            */

            return $"- {name} ({string.Join('/', roles)})";
        }
    }
}
