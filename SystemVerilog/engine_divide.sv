/*
if D = 0 then error(DivisionByZeroException) end
Q := 0                  -- Initialize quotient and remainder to zero
R := 0                     
for i := n − 1 .. 0 do  -- Where n is number of bits in N
  R := R << 1           -- Left-shift R by 1 bit
  R(0) := N(i)          -- Set the least-significant bit of R equal to bit i of the numerator
  if R ≥ D then
    R := R − D
    Q(i) := 1
  end
end
-- Where: N = Numerator, D = Denominator, n = #bits, R = Partial remainder, q(i) = bit #i of quotient
*/
module engine_divide # (
            parameter            WIDTH = 32
)
(
   input    logic                clock,
   input    logic                clock_sreset,
   input    logic [WIDTH-1:0]    numerator,
   input    logic [WIDTH-1:0]    denominator,
   input    logic                go,
   output   logic                busy,
   output   logic                result_valid,
   output   logic [WIDTH-1:0]    quotient,
   output   logic [WIDTH-1:0]    remainder
);
            localparam           WIDTHD = (WIDTH * 2);
            localparam           WIDTHC = $clog2(WIDTH + 1);
   enum     logic [2:0]          {S1, S2, S3} fsm;
            logic [WIDTH-1:0]    denominator_reg, numerator_reg;
            logic [WIDTHC-1:0]   count;
            wire  [WIDTH-1:0]    remainder_calc = {remainder[WIDTH-2:0], numerator_reg[WIDTH-1]} - denominator_reg;
   always_ff @ (posedge clock) begin
      if (clock_sreset) begin
         numerator_reg <= {WIDTH{1'bx}};
         denominator_reg <= {WIDTH{1'bx}};
         quotient <= {WIDTH{1'bx}};
         remainder <= {WIDTH{1'bx}};
         busy <= 1'bx;
         count <= {WIDTHC{1'bx}};
         result_valid <= 1'b0;
         fsm <= S1;
      end
      else begin
         case (fsm)
            S1 : begin
               numerator_reg <= numerator;
               denominator_reg <= denominator;
               remainder <= {WIDTH{1'b0}};
               count <= WIDTH[WIDTHC-1:0] - {{WIDTHC-1{1'b0}}, 1'b1};
               busy <= go;
               result_valid <= 1'b0;
               if (go)
                  fsm <= S2;
            end
            S2 : begin
               result_valid <= ~|count;
               if (~|count) begin
                  busy <= 1'b0;
                  fsm <= S1;
               end
               count <= count - {{WIDTHC-1{1'b0}}, 1'b1};
               numerator_reg <= {numerator_reg[WIDTH-2:0], 1'b0};
               quotient <= {quotient[WIDTH-2:0], ~remainder_calc[WIDTH-1]};
               if (~remainder_calc[WIDTH-1])
                  remainder <= remainder_calc;
               else
                  remainder <= {remainder[WIDTH-2:0], numerator_reg[WIDTH-1]};
            end            
         endcase
      end
   end
endmodule
