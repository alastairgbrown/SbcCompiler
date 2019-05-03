module engine # (
            parameter            WIDTHA = 12,
            parameter            WIDTHD = 32,
            parameter            ISA = 5,
            parameter            RESET_ADDR = 'h24,
            parameter            IRQ_ADDR = 'h20,
            parameter            STACK_ADDR = 'h1e0,
            parameter            MULT_SIZE = 32,
            parameter            BARREL_SHIFT = 0
)
(
   input    logic                clock,
   input    logic                clock_sreset,
   output   logic [WIDTHA-1:0]   address,
   output   logic [WIDTHD-1:0]   writedata,
   input    logic [WIDTHD-1:0]   readdata,
   output   logic                read,
   output   logic                write,
   input    logic                waitrequest,
   input    logic [WIDTHD-1:0]   irq
);
            localparam           OP = (WIDTHD / ISA);
            localparam           WIDTHI = (OP * ISA);
            localparam           WIDTHS = $clog2(OP);
            localparam           WIDTHP = (WIDTHA + WIDTHS);
            localparam           WIDTHB = $clog2(WIDTHD);
            localparam           WIDTHM = (MULT_SIZE * 2);
            localparam           ZERO = 128'h0;
            localparam           ONE = 128'h1;
            localparam           DONTCARE = 128'bx;
   enum     logic [1:0]          {FETCH, EXECUTE, WRITE} state;
   typedef  logic [ISA-1:0]      itype;
            itype [OP-1:0]       ir;
            itype                instr;
            logic [WIDTHD-1:0]   ra, rb, rc, rx, ry, rk, ri, rt;
            logic [WIDTHD-1:0]   ra_offset, rx_offset, ry_offset;
            logic [WIDTHD-1:0]   prev_ra, next_rc, prev_rc, reg_mux[12:0];
            logic [WIDTHA-1:0]   pc, next_pc;
            logic [WIDTHS-1:0]   slot, next_slot;
            logic [WIDTHD-1:0]   alu[9:0], alu_result, div_rem, div_quot;
            logic [WIDTHD+1:0]   adder;
            logic [WIDTHD*2-1:0] mult_result;
            logic [63:0]         opcode;
            logic [2:0]          opbits;
            logic [3:0]          alu_select, ra_sel, rb_sel, rc_sel;
            logic                pf, ef, ff, irqf, irq_event;
            logic                alu_cout, alu_imm, alu_mem;
            logic                ra_zero, last_slot, mem_op, imm_op, alu_op;
            logic                read_complete, write_complete, mem_complete;
            logic                NOP, JSR, JPZ, JMP, DUP, SWP, POP, PSH;
            logic                EXT, ZEQ, IRQ;
            logic                PUY, STY, LDY, AKX, POX, PUX, STX, LDX;
            logic                MBD, MFD, AKA, STA, LDA, AKY, POY;
            logic                DIV, MLT, XOR, IOR, AND, AGB, SUB, ADD;
            logic                F2I, I2F, FPD, FPM, FPA, SHL, SHR;
            logic                PFX, NUL;

            logic                div_go, div_busy, div_valid;
   engine_divide                 # (
                                    .WIDTH(WIDTHD)
                                 )
                                 divide (
                                    .clock(clock),
                                    .numerator(ra),
                                    .denominator(rb),
                                    .go(div_go),
                                    .busy(div_busy),
                                    .result_valid(div_valid),
                                    .quotient(div_quot),
                                    .remainder(div_rem)
                                 );
                                 
            logic                mult_go, mult_busy, mult_valid;
   engine_multiply               # (
                                    .WIDTH(WIDTHD)
                                 )
                                 multiply (
                                    .clock(clock),
                                    .dataa(ra),
                                    .datab(rb),
                                    .go(mult_go),
                                    .busy(mult_busy),
                                    .result_valid(mult_valid),
                                    .result(mult_result)
                                 );
            
   always_comb begin
      ra_offset = ra + rk;
      rx_offset = rx + rk;
      ry_offset = ry + rk;
      writedata = STA ? rb : (MFD | MBD) ? rt : ra;
      read_complete = read & ~waitrequest;
      write_complete = write & ~waitrequest;
      mem_complete = (read_complete | write_complete);
      irq_event = |irq & ~pf & ~ef & ~ff & ~irqf;
      instr = ir[slot];
      opbits = (ISA == 6) ? instr[5:3] : ef ? {1'b1, instr[4:3]} : {2'b01, instr[3]};
      opcode = 64'h1 << {opbits, instr[2:0]};
      {PFX, PFX, PFX, PFX, PFX, PFX, PFX, PFX} = opcode[7:0];
      {PFX, PFX, PFX, PFX, PFX, PFX, PFX, PFX} = opcode[15:8];
      {NOP, JSR, JPZ, JMP, DUP, SWP, POP, PSH} = opcode[23:16];
      {EXT, NUL, NUL, NUL, NUL, NUL, ZEQ, IRQ} = opcode[31:24];
      {PUY, STY, LDY, AKX, POX, PUX, STX, LDX} = opcode[39:32];
      {NUL, MBD, MFD, AKA, STA, LDA, AKY, POY} = opcode[47:40];
      {DIV, MLT, XOR, IOR, AND, AGB, SUB, ADD} = opcode[55:48];
      {NUL, F2I, I2F, FPD, FPM, FPA, SHL, SHR} = opcode[63:56];
      alu_op = ADD | SUB | AGB | AND | IOR | XOR | ZEQ;
      mem_op = (LDA | STA | LDX | STX | LDY | STY | MFD | MBD);
      imm_op = ~mem_op | (DIV & div_valid) | (MLT & mult_valid);
      ra_zero = ~|ra;
      adder = {1'b0, rb, 1'b1} + {1'b0, ra ^ {WIDTHD{SUB | AGB}}, (SUB | AGB)};
      alu[0] = adder[WIDTHD:1];
      alu[1] = {WIDTHD{adder[WIDTHD+1]}};
      alu[2] = rb & ra;
      alu[3] = rb | ra;
      alu[4] = rb ^ ra;
      alu[5] = {WIDTHD{ra_zero}};
      alu[6] = ra << ((BARREL_SHIFT != 0) ? rb[WIDTHB-1:0] : 1'b1);
      alu[7] = ra >> ((BARREL_SHIFT != 0) ? rb[WIDTHB-1:0] : 1'b1);
      alu[8] = mult_result[WIDTHD*2-1:WIDTHD];
      alu[9] = div_rem;
      alu_select = AGB ? 4'h1 : AND ? 4'h2 : IOR ? 4'h3 : XOR ? 4'h4 : ZEQ ? 4'h5 :
         SHL ? 4'h6 : SHR ? 4'h7 : MLT ? 4'h8 : DIV ? 4'h9 : 4'h0;
      alu_result = alu[alu_select];
      last_slot = (slot >= (OP - 1));
      next_pc = pc + last_slot;
      next_slot = last_slot ? ZERO[WIDTHS-1:0] : (slot + ONE[WIDTHS-1:0]);
      next_rc = rc + ONE[WIDTHD-1:0];
      prev_rc = rc - ONE[WIDTHD-1:0];
      prev_ra = ra - ONE[WIDTHD-1:0];
   end
   
   always_comb begin
      reg_mux[0] = ra;
      reg_mux[1] = rb;
      reg_mux[2] = rc;
      reg_mux[3] = rk;
      reg_mux[4] = {next_pc, next_slot};
      reg_mux[5] = rx;
      reg_mux[6] = ry;
      reg_mux[7] = ri;
      reg_mux[8] = alu_result;
      reg_mux[9] = readdata;
      reg_mux[10] = ra_offset;
      reg_mux[11] = mult_result[WIDTHD-1:0];
      reg_mux[12] = div_quot;
      ra_sel = AKA ? 4'ha : PSH ? 4'h3 : POP|STY|SWP|STX ? 4'h1 : JSR ? 4'h4 : IRQ ? 4'h7 :
         alu_op ? 4'h8 : LDX|LDY|LDA ? 4'h9 : PUX ? 4'h5 : PUY ? 4'h6 : STA ? 4'h2 : 4'h0;
      rb_sel = POP|STY|STX|POX|POY ? 4'h2 : PSH|SWP|DUP|IRQ|LDX|LDY|PUX|PUY ? 4'h0 :
         MLT ? 4'hb : DIV ? 4'hc : 4'h1;
      rc_sel = PSH|DUP|IRQ|LDX|LDY|PUX|PUY ? 4'h1 : 4'h2;
      {div_go, mult_go} = 2'b00;
      if (state == EXECUTE)
         {div_go, mult_go} = {DIV, MLT};
   end

   always_ff @ (posedge clock) begin
      if (clock_sreset) begin
         address <= DONTCARE[WIDTHA-1:0];
         read <= 1'b0;
         write <= 1'b0;
      end
      else
         case (state)
            FETCH : begin
               address <= pc;
               write <= 1'b0;
               if (~irq_event) begin
                  read <= ~read_complete;
               end
            end
            EXECUTE : begin
               address <= (LDX | STX) ? rx_offset[WIDTHA-1:0] :
                  (LDY | STY) ? ry_offset[WIDTHA-1:0] :
                  (LDA | STA) ? ra_offset[WIDTHA-1:0] : rb[WIDTHA-1:0];
               read <= read_complete ? 1'b0 : (LDX | LDY | LDA | MFD | MBD);
               write <= write_complete ? 1'b0 : (STX | STY | STA);
            end
            WRITE : begin
               address <= rc[WIDTHA-1:0];
               write <= ~write_complete;
            end
         endcase
   end

   always_ff @ (posedge clock) begin
      if (clock_sreset) begin
         pc <= RESET_ADDR[WIDTHA-1:0];
         ra <= DONTCARE[WIDTHD-1:0];
         rb <= DONTCARE[WIDTHD-1:0];
         rc <= DONTCARE[WIDTHD-1:0];
         rx <= DONTCARE[WIDTHD-1:0];
         ry <= DONTCARE[WIDTHD-1:0];
         rk <= DONTCARE[WIDTHD-1:0];
         ri <= DONTCARE[WIDTHD-1:0];
         rt <= DONTCARE[WIDTHD-1:0];
         ir <= DONTCARE[OP*ISA-1:0];
         pf <= 1'b0;
         ef <= 1'b0;
         ff <= 1'b0;
         irqf <= 1'b0;
         slot <= ZERO[WIDTHS-1:0];
         state <= FETCH;
      end
      else begin
         case (state)
            FETCH : begin
               ir <= readdata[WIDTHI-1:0];
               if (irq_event) begin
                  irqf <= 1'b1;
                  ri <= {pc, slot};
                  {pc, slot} <= {IRQ_ADDR[WIDTHA-1:0], ZERO[WIDTHS-1:0]};
               end
               else begin
                  ff <= 1'b1;
                  if (read_complete) begin
                     state <= EXECUTE;
                  end
               end
            end
            EXECUTE : begin
               ff <= 1'b0;
               pf <= PFX;
               ef <= EXT;
               rk <= PFX ? (pf ? {rk[WIDTHD-5:0], instr[3:0]} :
                  {{WIDTHD-6{instr[3]}}, instr[3:0]}) : ZERO[WIDTHD-1:0];
               if (NUL | NOP) ;
               if (IRQ) irqf <= 1'b0;
               if (POX) rx <= ra;
               if (AKX) rx <= rx_offset;
               if (AKY) ry <= ry_offset;
               if (MFD | MBD) rt <= readdata;
               if (imm_op | mem_complete) begin
                  ra <= reg_mux[ra_sel];
                  rb <= reg_mux[rb_sel];
                  rc <= reg_mux[rc_sel];
                  if (JSR | JMP | (JPZ & ~ra_zero))
                     state <= FETCH;
                  else begin
                     if (MFD | MBD) begin
                        state <= WRITE;
                     end
                     else begin
                        {pc, slot} <= {next_pc, next_slot};
                        if (last_slot)
                           state <= FETCH;
                     end
                  end
               end
            end
            WRITE : begin
               if (write_complete) begin
                  ra <= prev_ra;
                  rc <= MBD ? prev_rc : next_rc;
                  {pc, slot} <= {next_pc, next_slot};
                  if (last_slot)
                     state <= FETCH;
                  else
                     state <= EXECUTE;
               end
            end
         endcase
      end
   end

endmodule
