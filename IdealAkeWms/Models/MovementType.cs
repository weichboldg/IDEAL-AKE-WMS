namespace IdealAkeWms.Models;

public enum MovementType
{
    Einbuchung = 0,
    Ausbuchung = 1,
    Umbuchung = 2,
    SageEinbuchung = 3,    // Sage-Korrektur Plus: WMS war zu niedrig
    SageAusbuchung = 4     // Sage-Korrektur Minus: WMS war zu hoch
}
