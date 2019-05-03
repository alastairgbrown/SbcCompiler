/*
ISA5     MSBIT 0 PFX 0x0..0xF
ISA6     MSBIT 0 PFX 0x0..0x1F

         MSBITS ISA5/6
                     EXT   EXT
ISA5      10    11    10    11
ISA6     100   101   110   111
         ---------------------
000      LDW   OVR   AND
001      STW   ASR   IOR
010      PSH   GEQ   XOR
011      POP   ZEQ   MLT
100      SWP   ADD   ROL
101      JNZ   ADC   ROR
110      JSR   SUB
111     (EXT)  NOP
*/
module micro3 # (
            parameter            WIDTHIA = 9,
            parameter            WIDTHID = 18,
            parameter            WIDTHDA = 10,
            parameter            WIDTHDD = 32,
            parameter            ISA = 6,
            parameter            OP_MASK = 32'hffffffff,
            parameter            RESET_ADDR = 'h24,
            parameter            IRQ_ADDR = 'h20,
            parameter            STACK_ADDR = 'h1e0,
            parameter            MULT_SIZE = 18,
            parameter            ROTATE_BITS = $clog2(WIDTHDD)
)
(
   input    logic                clock,
   input    logic                clock_sreset,
   output   logic [WIDTHIA-1:0]  iaddress,
   input    logic [WIDTHID-1:0]  ireaddata,
   output   logic                iread,
   input    logic                iwaitrequest,
   output   logic [WIDTHDA-1:0]  daddress,
   output   logic [WIDTHDD-1:0]  dwritedata,
   input    logic [WIDTHDD-1:0]  dreaddata,
   output   logic                dread,
   output   logic                dwrite,
   input    logic                dwaitrequest,
   input    logic [WIDTHDD-1:0]  irq
);
            localparam           WIDTHR = ROTATE_BITS;
            localparam           OP = (WIDTHID / ISA);
            localparam           WIDTHI = (OP * ISA);
            localparam           WIDTHS = $clog2(OP);
            localparam           WIDTHP = (WIDTHIA + WIDTHS);
            localparam           WIDTHM = MULT_SIZE * 2;
            localparam           WIDTHL = ((WIDTHDD + WIDTHDA) > (WIDTHID + WIDTHIA)) ? (WIDTHDD + WIDTHDA) : (WIDTHID + WIDTHIA);
            localparam           ZERO = {WIDTHL{1'b0}};
            localparam           ONE = {ZERO, 1'b1};
            localparam           DONTCARE = {WIDTHL{1'bx}};
   enum     logic [2:0]          {FETCH, EXECUTE, STACK1} state;
   typedef  logic [ISA-1:0]      itype;
            itype [OP-1:0]       ir;
            itype                instr;
            logic [WIDTHDD-1:0]  rt, rn, alu_result; 
            logic [WIDTHIA-1:0]  pc, next_pc;
            logic [WIDTHDA-1:0]  sp, next_sp, prev_sp;
            logic [WIDTHS-1:0]   slot, next_slot;
            logic [WIDTHDD+1:0]  alu_adder;
            logic [WIDTHDD*2-1:0] alu_mult;
            logic [WIDTHDD:0]    alu[9:0];
            logic [31:0]         opcode;
            logic [1:0]          opbits;
            logic [3:0]          alu_select;
            logic [1:0]          sf;
            logic                cf, ef, pf, ff, irqf, popflag;
            logic                alu_cout, alu_imm, alu_mem, irq_event;
            logic                rt_zero, last_slot, imm_op, alu_done, sp_inc, sp_dec;
            logic                dread_complete, dwrite_complete, dmem_complete, iread_complete;
            logic                EXT, JSR, JNZ, SWP, POP, PSH, STW, LDW;
            logic                NOP, SUB, ADC, ADD, ZEQ, GEQ, ASR, OVR;
            logic                ROR, ROL, MLT, XOR, IOR, AND;
            logic                PFX, NUL;
            
   always_comb begin
      iaddress = pc;
      dwritedata = rn;
      dread_complete = dread & ~dwaitrequest;
      dwrite_complete = dwrite & ~dwaitrequest;
      iread_complete = iread & ~iwaitrequest;
      dmem_complete = (dread_complete | dwrite_complete);
      irq_event = |irq & ~irqf & ~ff & ~pf & ~|sf & ((ISA == 5) ? ~ef : 1'b1);
      instr = ir[slot];
      opbits = (ISA == 6) ? instr[4:3] : {ef, instr[3]};
      PFX = (ISA == 6) ? ~instr[ISA-1] : (~instr[ISA-1] & ~ef);
      opcode = ({31'h0, instr[ISA-1]} << {opbits, instr[2:0]}) & OP_MASK[31:0];
      {EXT, JSR, JNZ, SWP, POP, PSH, STW, LDW} = opcode[7:0];
      {NOP, SUB, ADC, ADD, ZEQ, GEQ, ASR, OVR} = opcode[15:8];
      {NUL, NUL, ROR, ROL, MLT, XOR, IOR, AND} = opcode[23:16];
      {NUL, NUL, NUL, NUL, NUL, NUL, NUL, NUL} = opcode[31:24];
      alu_imm = ASR | ZEQ | MLT | GEQ;
      alu_mem = XOR | IOR | AND | SUB | ADC | ADD;
      imm_op = NUL | NOP | (PFX /*& pf*/) | JSR | SWP | ROR | ROL | alu_imm | (EXT & (ISA == 5));
      rt_zero = ~|rt;
      alu_adder = {1'b0, rn, 1'b1} + {1'b0, (rt ^ {WIDTHDD{SUB}}), ((cf & ADC) | SUB)};
      alu_mult = rn[MULT_SIZE-1:0] * rt[MULT_SIZE-1:0];
      alu[0] = {alu_adder[WIDTHDD+1]^SUB, alu_adder[WIDTHDD:1]}; // add, adc, sub
      alu[1] = {cf, {WIDTHDD{~rt[WIDTHDD-1]}}};   // rt >= 0
      alu[2] = {cf, rn & rt};
      alu[3] = {cf, rn | rt};
      alu[4] = {cf, rn ^ rt};
      alu[5] = {cf, {WIDTHDD{rt_zero}}};
      alu[6] = {rt[0], rt[WIDTHDD-1], rt[WIDTHDD-1:1]};
      alu[7] = alu_mult[WIDTHDD*2-1:WIDTHDD];
      alu[8] = rt <<< rn[WIDTHR-1:0];
      alu[9] = rt >>> rn[WIDTHR-1:0];
      alu_select = GEQ ? 4'h1 : AND ? 4'h2 : IOR ? 4'h3 : XOR ? 4'h4 :
         ZEQ ? 4'h5 : ASR ? 4'h6 : MLT ? 4'h7 : ROL ? 4'h8 : ROR ? 4'h9 : 4'h0;
      {alu_cout, alu_result} = alu[alu_select];
      last_slot = (slot >= (OP - 1));
      next_pc = pc + last_slot;
      next_slot = last_slot ? ZERO[WIDTHS-1:0] : (slot + ONE[WIDTHS-1:0]);
      next_sp = sp + ONE[WIDTHDA-1:0];
      prev_sp = sp - ONE[WIDTHDA-1:0];
   end

   always_ff @ (posedge clock) begin
      if (clock_sreset) begin
         iread <= 1'b0;
         daddress <= DONTCARE[WIDTHDA-1:0];
         dwrite <= 1'b0;
         dread <= 1'b0;
         pc <= RESET_ADDR[WIDTHIA-1:0];
         sp <= STACK_ADDR[WIDTHDA-1:0];
         rt <= DONTCARE[WIDTHDD-1:0];
         rn <= DONTCARE[WIDTHDD-1:0];
         ir <= DONTCARE[OP*ISA-1:0];
         cf <= DONTCARE[0];
         ef <= 1'b0;
         pf <= 1'b0;
         ff <= 1'b0;
         popflag <= 1'b0;
         sf <= 2'b00;
         irqf <= 1'b0;
         slot <= ZERO[WIDTHS-1:0];
         state <= FETCH;
      end
      else begin
         case (state)
            FETCH : begin
               daddress <= next_sp;
               ir <= ireaddata[WIDTHI-1:0];
               if (irq_event) begin
                  dwrite <= ~dwrite_complete;
                  if (dwrite_complete) begin
                     {pc, slot} <= {IRQ_ADDR[WIDTHIA-1:0], ZERO[WIDTHS-1:0]};
                     rt <= {pc, slot};
                     rn <= rt;
                     irqf <= 1'b1;
                  end
               end
               else begin
                  iread <= ~iread_complete;
                  ff <= 1'b1;
                  if (iread_complete) begin
                     state <= EXECUTE;
                  end
               end
            end
            EXECUTE : begin
               popflag <= 1'b0;
               ff <= 1'b0;
               daddress <= (LDW | STW) ? rt[WIDTHDA-1:0] : (JNZ | POP | alu_mem) ? sp : next_sp;
               dread <= (LDW | JNZ | POP | alu_mem) & ~dread_complete;
               //dwrite <= (STW | PSH | OVR | (PFX & ~pf)) & ~dwrite_complete;
               dwrite <= (STW | PSH | OVR) & ~dwrite_complete;
               //if (PFX & pf) rt <= {rt[WIDTHDD-ISA:0], instr[ISA-2:0]};
               pf <= PFX;
               if (PFX) rt <= pf ? {rt[WIDTHDD-ISA:0], instr[ISA-2:0]} : {{WIDTHDD-ISA+1{instr[ISA-2]}}, instr[ISA-2:0]};
               if (MLT) rn <= alu_mult[WIDTHDD-1:0];
               if (imm_op | dmem_complete) begin
                  sf <= SWP ? {sf[0], 1'b1} : 2'b00;
                  if (ISA == 5) ef <= EXT;
                  //if (PFX & dmem_complete) rt <= {{WIDTHDD-ISA+1{instr[ISA-2]}}, instr[ISA-2:0]};
                  if (PSH | OVR | SWP | (PFX & ~pf)) rn <= rt;
                  if (OVR | POP | SWP | JNZ) rt <= rn;
                  if (PSH | OVR) sp <= next_sp;
                  if (POP | JNZ | alu_mem) {rn, sp} <= {dreaddata, prev_sp};
                  if (JSR) rt <= {next_pc, next_slot};
                  if (alu_imm | alu_mem) {rt, cf} <= {alu_result, alu_cout};
                  if (LDW) rt <= dreaddata;
                  if (STW) begin
                     daddress <= sp;
                     dread <= 1'b1;
                     state <= STACK1;
                  end
                  else begin
                     if ((JNZ & ~rt_zero) | JSR)
                        {pc, slot, irqf} <= {rt[WIDTHP-1:0], sf[1] ? 1'b0 : irqf};
                     else
                        {pc, slot} <= {next_pc, next_slot};
                     if ((JNZ & ~rt_zero) | JSR)
                        state <= FETCH;
                     else
                        if (last_slot)
                           state <= FETCH;
                  end
               end
            end
            STACK1 : begin
               if (dread_complete) begin
                  daddress <= prev_sp;
                  sp <= prev_sp;
                  rt <= rn;
                  rn <= dreaddata;
                  if (popflag) begin
                     dread <= 1'b0;
                     {pc, slot} <= {next_pc, next_slot};
                     if (last_slot)
                        state <= FETCH;
                     else
                        state <= EXECUTE;
                  end
                  else
                     popflag <= 1'b1;
               end
            end
         endcase
      end
   end

endmodule
