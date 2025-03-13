using EXGuard.Core.Services;
using EXGuard.DynCipher.AST;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EXGuard.DynCipher.Generation
{
    internal class StatementGenerator
    {


        static LoopStatement GenerateInverse(LoopStatement encodeLoop, Expression var, [EXGuard.Core.Helpers.System.Runtime.CompilerServices.TupleElementNames(new string[] { "encode", "inverse" })] Dictionary<AssignmentStatement, ValueTuple<Expression, Expression>> assignments)
        {
            var decodeLoop = new LoopStatement()
            {
                Begin = encodeLoop.Begin,
                Limit = encodeLoop.Limit,
            };

            foreach (var assignment in assignments.Reverse())
            {
                decodeLoop.Statements.Add(new AssignmentStatement
                {
                    Target = var,
                    Value = assignment.Value.Item2
                });
            }

            return decodeLoop;
        }

        public static void GeneratePair(RandomGenerator random, Expression var, Expression result, int depth, out LoopStatement statement, out LoopStatement inverse)
        {

            statement = new LoopStatement
            {
                Begin = 0,
                Limit = depth,
            };

            Dictionary<AssignmentStatement, ValueTuple<Expression, Expression>> assignments = new Dictionary<AssignmentStatement, ValueTuple<Expression, Expression>>();

            for (int i = 0; i < depth; i++)
            {
                ExpressionGenerator.GeneratePair(random, var, result, depth, out var expression, out var inverseExpression);

                var assignment = new AssignmentStatement
                {
                    Target = var,
                    Value = expression
                };

                assignments.Add(assignment, new ValueTuple<Expression, Expression>(expression, inverseExpression));
                statement.Statements.Add(assignment);
            }

            inverse = GenerateInverse(statement, result, assignments);
        }
    }
}
