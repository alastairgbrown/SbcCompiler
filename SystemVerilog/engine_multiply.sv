module engine_multiply
# (
            parameter            WIDTH = 32
)
(
   input    logic                clock,
   input    logic                clock_sreset,
   input    logic [WIDTH-1:0]    dataa,
   input    logic [WIDTH-1:0]    datab,
   input    logic                go,
   output   logic                busy,
   output   logic                result_valid,
   output   logic [WIDTH*2-1:0]  result
);
            localparam           WIDTHD = (WIDTH * 2);
            localparam           WIDTHC = $clog2(WIDTH);
   enum     logic [1:0]          {S1, S2} fsm;
            logic [WIDTH-1:0]    dataa_reg;
            logic [WIDTHD-1:0]   datab_reg;
            logic [WIDTHC-1:0]   count;
   always_ff @ (posedge clock) begin
      if (clock_sreset) begin
         fsm <= S1;
      end
      else begin
         case (fsm)
            S1 : begin
               result_valid <= 1'b0;
               dataa_reg <= dataa;
               datab_reg <= datab;
               result <= {WIDTHD{1'b0}};
               count <= WIDTH[WIDTHC-1:0] - {{WIDTHC-1{1'b0}}, 1'b1};
               busy <= go;
               if (go)
                  fsm <= S2;
            end
            S2 : begin
               if ((~|count) | (~|dataa_reg)) begin
                  busy <= 1'b0;
                  result_valid <= 1'b1;
                  fsm <= S1;
               end
               count <= count - {{WIDTHC-1{1'b0}}, 1'b1};
               if (dataa_reg[0])
                  result <= result + datab_reg;
               dataa_reg <= dataa_reg >> 1;
               datab_reg <= datab_reg << 1;
            end
         endcase
      end
   end

endmodule
