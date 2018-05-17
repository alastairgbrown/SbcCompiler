// ReSharper disable InconsistentNaming
namespace SbcLibrary
{
    public enum Opcode
    {
        PFX0 = 0x00,    PSH = 0x10,     LDX = 0x20,      ADD = 0x30,     
        PFX1 = 0x01,    POP = 0x11,     STX = 0x21,      SUB = 0x31,
        PFX2 = 0x02,    SWP = 0x12,     SWX = 0x22,      AGB = 0x32,
        PFX3 = 0x03,    DUP = 0x13,     AKX = 0x23,      AND = 0x33,
        PFX4 = 0x04,    JMP = 0x14,     LDY = 0x24,      IOR = 0x34,
        PFX5 = 0x05,    JPZ = 0x15,     STY = 0x25,      XOR = 0x35,
        PFX6 = 0x06,    JSR = 0x16,     SWY = 0x26,      MLT = 0x36,
        PFX7 = 0x07,    NOP = 0x17,     AKY = 0x27,      DIV = 0x37,
        PFX8 = 0x08,    IRQ = 0x18,     LDA = 0x28,      SHR = 0x38,
        PFX9 = 0x09,    ZEQ = 0x19,     STA = 0x29,      SHL = 0x39,
        PFXA = 0x0A,                    AKA = 0x2A,       
        PFXB = 0x0B,                    MFD = 0x2B,      FPA = 0x3B,
        PFXC = 0x0C,                    MBD = 0x2C,      FPM = 0x3C,
        PFXD = 0x0D,                                     FPD = 0x3D,
        PFXE = 0x0E,                                     I2F = 0x3E,
        PFXF = 0x0F,    EXT = 0x1F,                      F2I = 0x3F,
    }
}